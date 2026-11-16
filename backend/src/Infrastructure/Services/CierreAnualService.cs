using FondoNet.Application.Cierre;
using FondoNet.Application.Common;
using FondoNet.Domain.Entities;
using FondoNet.Domain.Enums;
using FondoNet.Domain.Finance;
using FondoNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FondoNet.Infrastructure.Services;

public class CierreAnualService(FondoNetDbContext db) : ICierreAnualService
{
    public async Task<bool> EstaCerradoAsync(int anio, CancellationToken ct = default)
    {
        var cajasActivas = await db.CajasAhorro
            .AsNoTracking()
            .Where(c => c.Activa)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (cajasActivas.Count == 0)
            return false;

        int cerradas = await db.CierresAnuales
            .AsNoTracking()
            .CountAsync(c => c.Anio == anio && cajasActivas.Contains(c.CajaAhorroId), ct);

        return cerradas >= cajasActivas.Count;
    }

    public async Task<ResultadoCierreAnualDto> EjecutarAsync(int anio, bool simulacion, CancellationToken ct = default)
    {
        if (anio < 2000 || anio > 2100)
            throw new ReglaNegocioException("El año del ejercicio no es válido.");

        if (!simulacion && anio >= DateTime.Today.Year)
            throw new ReglaNegocioException($"No se puede ejecutar el cierre definitivo del ejercicio {anio} porque el año aún no ha concluido. Use el modo simulación.");

        DateOnly inicio = new(anio, 1, 1);
        DateOnly fin = new(anio, 12, 31);

        var cajas = await db.CajasAhorro
            .AsNoTracking()
            .Where(c => c.Activa)
            .OrderBy(c => c.Id)
            .ToListAsync(ct);

        var cierresExistentes = await db.CierresAnuales
            .AsNoTracking()
            .Where(c => c.Anio == anio)
            .ToDictionaryAsync(c => c.CajaAhorroId, ct);

        bool todasCerradas = cajas.Count > 0 && cajas.All(c => cierresExistentes.ContainsKey(c.Id));
        if (todasCerradas && !simulacion)
        {
            return await ConstruirResultadoCerradoAsync(anio, simulacion: false, cajas, fin, ct);
        }

        // Movimientos hasta el 31/dic excluyendo RENDIMIENTO/RETIRO de cierre del propio año (para que la simulación de un año ya cerrado reproduzca el cálculo).
        string conceptoRetiroAnual = $"Liquidación del ciclo {anio}";
        var ahorradores = await db.Ahorradores
            .AsNoTracking()
            .Where(a => cajas.Select(c => c.Id).Contains(a.CajaAhorroId))
            .OrderBy(a => a.Id)
            .ToListAsync(ct);

        var movimientos = await db.MovimientosAhorro
            .AsNoTracking()
            .Where(m => m.Fecha <= fin
                     && !(m.Fecha >= inicio && m.Tipo == TipoMovimientoAhorro.Rendimiento)
                     && !(m.Fecha == fin && m.Tipo == TipoMovimientoAhorro.Retiro && m.Concepto == conceptoRetiroAnual))
            .OrderBy(m => m.Fecha)
            .ToListAsync(ct);

        var bolsasPorCaja = await db.Amortizaciones
            .AsNoTracking()
            .Where(am => am.Pagada && am.FechaVencimiento >= inicio && am.FechaVencimiento <= fin)
            .GroupBy(am => am.Prestamo!.Ahorrador!.CajaAhorroId)
            .Select(g => new { CajaId = g.Key, Bolsa = g.Sum(x => x.Interes) })
            .ToDictionaryAsync(x => x.CajaId, x => x.Bolsa, ct);

        var movimientosPorAhorrador = movimientos
            .GroupBy(m => m.AhorradorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var resultadoCajas = new List<CierreCajaDto>(cajas.Count);
        var nuevosMovimientos = new List<MovimientoAhorro>();
        var nuevosCierres = new List<CierreAnual>();

        foreach (var caja in cajas)
        {
            var ahorradoresCaja = ahorradores.Where(a => a.CajaAhorroId == caja.Id).ToList();
            decimal bolsa = bolsasPorCaja.GetValueOrDefault(caja.Id, 0m);

            var factores = ahorradoresCaja
                .Select(a =>
                {
                    var movs = movimientosPorAhorrador.GetValueOrDefault(a.Id) ?? [];
                    decimal factor = RendimientosCalculator.CalcularFactor(
                        movs.Select(m => new MovimientoSaldo(m.Fecha, m.Monto)), inicio, fin);
                    return new FactorAhorrador(a.Id, factor);
                })
                .ToList();

            var reparto = RendimientosCalculator
                .Repartir(bolsa, factores)
                .ToDictionary(r => r.AhorradorId, r => r.Monto);

            var detalleAhorradores = new List<CierreAhorradorDto>(ahorradoresCaja.Count);
            decimal totalRendCaja = 0m;
            decimal totalRetiroCaja = 0m;

            foreach (var a in ahorradoresCaja)
            {
                decimal factor = factores.First(f => f.AhorradorId == a.Id).Factor;
                decimal rendimiento = reparto.GetValueOrDefault(a.Id, 0m);
                decimal saldoPrevio = (movimientosPorAhorrador.GetValueOrDefault(a.Id) ?? []).Sum(m => m.Monto);

                bool activoAlCierre = a.FechaIngreso <= fin && (a.FechaBaja is null || a.FechaBaja > fin);
                decimal retiro = activoAlCierre && saldoPrevio + rendimiento > 0m
                    ? saldoPrevio + rendimiento
                    : 0m;

                totalRendCaja += rendimiento;
                totalRetiroCaja += retiro;

                detalleAhorradores.Add(new CierreAhorradorDto(
                    a.Id, a.Nombre, Dinero.Redondear(factor), rendimiento, retiro));

                if (!simulacion && !cierresExistentes.ContainsKey(caja.Id))
                {
                    if (rendimiento > 0m)
                    {
                        nuevosMovimientos.Add(new MovimientoAhorro
                        {
                            AhorradorId = a.Id,
                            Fecha = fin,
                            Tipo = TipoMovimientoAhorro.Rendimiento,
                            Monto = rendimiento,
                            Concepto = $"Rendimiento ejercicio {anio}"
                        });
                    }

                    if (retiro > 0m)
                    {
                        nuevosMovimientos.Add(new MovimientoAhorro
                        {
                            AhorradorId = a.Id,
                            Fecha = fin,
                            Tipo = TipoMovimientoAhorro.Retiro,
                            Monto = -retiro,
                            Concepto = conceptoRetiroAnual
                        });
                    }
                }
            }

            if (!simulacion && !cierresExistentes.ContainsKey(caja.Id))
            {
                nuevosCierres.Add(new CierreAnual
                {
                    CajaAhorroId = caja.Id,
                    Anio = anio,
                    FechaEjecucion = DateTime.UtcNow,
                    Bolsa = bolsa,
                    TotalRendimientos = totalRendCaja,
                    TotalRetiros = totalRetiroCaja
                });
            }

            resultadoCajas.Add(new CierreCajaDto(
                caja.Id,
                caja.Nombre,
                bolsa,
                Dinero.Redondear(factores.Where(f => f.Factor > 0).Sum(f => f.Factor)),
                totalRendCaja,
                totalRetiroCaja,
                detalleAhorradores));
        }

        if (!simulacion && (nuevosMovimientos.Count > 0 || nuevosCierres.Count > 0))
        {
            db.MovimientosAhorro.AddRange(nuevosMovimientos);
            db.CierresAnuales.AddRange(nuevosCierres);
            await db.SaveChangesAsync(ct);
        }

        return new ResultadoCierreAnualDto(
            anio,
            simulacion,
            todasCerradas,
            resultadoCajas.Sum(c => c.Bolsa),
            resultadoCajas.Sum(c => c.TotalRendimientos),
            resultadoCajas);
    }

    private async Task<ResultadoCierreAnualDto> ConstruirResultadoCerradoAsync(
        int anio, bool simulacion, List<CajaAhorro> cajas, DateOnly fin, CancellationToken ct)
    {
        // Reutiliza el cálculo en modo simulación pero marcando YaCerrado = true y Simulacion = simulacion.
        var sim = await EjecutarAsync(anio, simulacion: true, ct);
        return sim with { Simulacion = simulacion, YaCerrado = true };
    }
}

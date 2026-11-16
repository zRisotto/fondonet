using FondoNet.Application.Common;
using FondoNet.Application.Nomina;
using FondoNet.Domain.Entities;
using FondoNet.Domain.Enums;
using FondoNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FondoNet.Infrastructure.Services;

public class NominaService(FondoNetDbContext db, INominaParser parser) : INominaService
{
    public async Task<ResultadoImportacionNomina> ImportarAsync(
        int cajaAhorroId, Stream archivo, string nombreArchivo, CancellationToken ct = default)
    {
        bool cajaExiste = await db.CajasAhorro.AnyAsync(c => c.Id == cajaAhorroId, ct);
        if (!cajaExiste)
            throw new NoEncontradoException($"La caja de ahorro {cajaAhorroId} no existe.");

        var parseo = parser.Parsear(archivo, nombreArchivo);
        var errores = parseo.Errores.ToList();
        var cambios = new List<CambioAhorrador>();

        var ahorradores = await db.Ahorradores
            .Where(a => a.CajaAhorroId == cajaAhorroId)
            .ToDictionaryAsync(a => a.ClaveEmpleado, StringComparer.OrdinalIgnoreCase, ct);

        foreach (var reg in parseo.Registros)
        {
            if (!ahorradores.TryGetValue(reg.ClaveEmpleado, out var ahorrador))
            {
                errores.Add(new ErrorRenglon(reg.Renglon, $"El ahorrador con clave '{reg.ClaveEmpleado}' no existe en la caja {cajaAhorroId}."));
                continue;
            }

            if (reg.TipoMovimiento == TipoMovimientoNomina.Baja && reg.Fecha < ahorrador.FechaIngreso)
            {
                errores.Add(new ErrorRenglon(reg.Renglon, $"La fecha de baja ({reg.Fecha}) es anterior a la fecha de ingreso ({ahorrador.FechaIngreso})."));
                continue;
            }

            string anterior = ahorrador.Estatus.ToString().ToUpperInvariant();

            switch (reg.TipoMovimiento)
            {
                case TipoMovimientoNomina.Alta:
                    ahorrador.Estatus = EstatusAhorrador.Activo;
                    ahorrador.FechaBaja = null;
                    break;
                case TipoMovimientoNomina.Baja:
                    ahorrador.Estatus = EstatusAhorrador.Inactivo;
                    ahorrador.FechaBaja = reg.Fecha;
                    await LiquidarPorBajaAsync(ahorrador, reg.Fecha, ct);
                    break;
                case TipoMovimientoNomina.Incapacidad:
                case TipoMovimientoNomina.Licencia:
                    ahorrador.Estatus = EstatusAhorrador.Licencia;
                    break;
            }

            ahorrador.SalarioBase = reg.SalarioBase;
            string nuevo = ahorrador.Estatus.ToString().ToUpperInvariant();

            cambios.Add(new CambioAhorrador(
                reg.Renglon,
                ahorrador.Id,
                ahorrador.ClaveEmpleado,
                reg.TipoMovimiento.ToString().ToUpperInvariant(),
                anterior,
                nuevo,
                ahorrador.SalarioBase));
        }

        if (cambios.Count > 0)
            await db.SaveChangesAsync(ct);

        return new ResultadoImportacionNomina(
            cajaAhorroId,
            nombreArchivo,
            parseo.Registros.Count + parseo.Errores.Count,
            cambios.Count,
            cambios,
            errores.OrderBy(e => e.Renglon).ToList());
    }

    /// <summary>Si el ahorrador dado de baja tiene saldo a favor, genera su RETIRO por baja laboral (igual que el paso 7 del seed).</summary>
    private async Task LiquidarPorBajaAsync(Ahorrador ahorrador, DateOnly fechaBaja, CancellationToken ct)
    {
        decimal saldo = await db.MovimientosAhorro
            .Where(m => m.AhorradorId == ahorrador.Id && m.Fecha <= fechaBaja)
            .SumAsync(m => (decimal?)m.Monto, ct) ?? 0m;

        if (saldo > 0m)
        {
            db.MovimientosAhorro.Add(new MovimientoAhorro
            {
                AhorradorId = ahorrador.Id,
                Fecha = fechaBaja,
                Tipo = TipoMovimientoAhorro.Retiro,
                Monto = -saldo,
                Concepto = "Liquidación por baja laboral"
            });
        }
    }
}

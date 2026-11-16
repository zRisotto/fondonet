using FondoNet.Application.Ahorradores;
using FondoNet.Application.Common;
using FondoNet.Application.Prestamos;
using FondoNet.Domain.Entities;
using FondoNet.Domain.Enums;
using FondoNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FondoNet.Infrastructure.Services;

public class AhorradorService(FondoNetDbContext db, IPrestamoService prestamoService) : IAhorradorService
{
    public async Task<IReadOnlyList<CajaAhorroResumenDto>> ListarCajasAsync(int? ahorradorId, CancellationToken ct = default)
    {
        var query = db.CajasAhorro
            .AsNoTracking()
            .Include(c => c.Empresa)
            .Where(c => c.Activa);

        if (ahorradorId.HasValue)
        {
            int? cajaId = await db.Ahorradores
                .AsNoTracking()
                .Where(a => a.Id == ahorradorId.Value)
                .Select(a => (int?)a.CajaAhorroId)
                .FirstOrDefaultAsync(ct)
                ?? throw new NoEncontradoException($"El ahorrador {ahorradorId.Value} no existe.");

            query = query.Where(c => c.Id == cajaId.Value);
        }

        var cajas = await query.OrderBy(c => c.Id).ToListAsync(ct);

        return cajas
            .Select(c => new CajaAhorroResumenDto(
                c.Id,
                c.Nombre,
                c.Empresa?.Nombre ?? string.Empty,
                c.TasaInteresMensual,
                c.PorcentajeAhorro))
            .ToList();
    }

    public async Task<IReadOnlyList<AhorradorResumenDto>> ListarAsync(int? cajaAhorroId, CancellationToken ct = default)
    {
        var query = db.Ahorradores
            .AsNoTracking()
            .Include(a => a.CajaAhorro)
                .ThenInclude(c => c!.Empresa)
            .AsQueryable();

        if (cajaAhorroId.HasValue)
            query = query.Where(a => a.CajaAhorroId == cajaAhorroId.Value);

        var ahorradores = await query.OrderBy(a => a.Id).ToListAsync(ct);

        int anioActual = DateTime.Today.Year;
        DateOnly inicioCiclo = new(anioActual, 1, 1);
        DateOnly finCiclo = new(anioActual, 12, 31);

        var saldos = await db.MovimientosAhorro
            .AsNoTracking()
            .Where(m => m.Fecha >= inicioCiclo && m.Fecha <= finCiclo)
            .GroupBy(m => m.AhorradorId)
            .Select(g => new { AhorradorId = g.Key, Saldo = g.Sum(m => m.Monto) })
            .ToDictionaryAsync(x => x.AhorradorId, x => x.Saldo, ct);

        return ahorradores
            .Select(a => MapearResumen(a, saldos.GetValueOrDefault(a.Id, 0m)))
            .ToList();
    }

    public async Task<IReadOnlyList<AvalDisponibleDto>> ListarAvalesDisponiblesAsync(int ahorradorId, CancellationToken ct = default)
    {
        var solicitante = await db.Ahorradores
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == ahorradorId, ct)
            ?? throw new NoEncontradoException($"El ahorrador {ahorradorId} no existe.");

        return await db.Ahorradores
            .AsNoTracking()
            .Where(a => a.CajaAhorroId == solicitante.CajaAhorroId
                     && a.Estatus == EstatusAhorrador.Activo
                     && a.Id != ahorradorId)
            .OrderBy(a => a.Nombre)
            .Select(a => new AvalDisponibleDto(a.Id, a.ClaveEmpleado, a.Nombre))
            .ToListAsync(ct);
    }

    public async Task<EstadoCuentaDto> ObtenerEstadoCuentaAsync(int ahorradorId, int? anio, CancellationToken ct = default)
    {
        var ahorrador = await db.Ahorradores
            .AsNoTracking()
            .Include(a => a.CajaAhorro)
                .ThenInclude(c => c!.Empresa)
            .FirstOrDefaultAsync(a => a.Id == ahorradorId, ct)
            ?? throw new NoEncontradoException($"El ahorrador {ahorradorId} no existe.");

        int anioActual = DateTime.Today.Year;
        int anioConsulta = anio ?? anioActual;
        DateOnly inicioAnio = new(anioConsulta, 1, 1);
        DateOnly finAnio = new(anioConsulta, 12, 31);

        var movimientos = await db.MovimientosAhorro
            .AsNoTracking()
            .Where(m => m.AhorradorId == ahorradorId && m.Fecha >= inicioAnio && m.Fecha <= finAnio)
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .ToListAsync(ct);

        decimal totalOrdinario = movimientos.Where(m => m.Tipo == TipoMovimientoAhorro.Ordinario).Sum(m => m.Monto);
        decimal totalPatronal = movimientos.Where(m => m.Tipo == TipoMovimientoAhorro.Patronal).Sum(m => m.Monto);
        decimal totalVoluntario = movimientos.Where(m => m.Tipo == TipoMovimientoAhorro.Voluntario).Sum(m => m.Monto);
        decimal totalRendimientos = movimientos.Where(m => m.Tipo == TipoMovimientoAhorro.Rendimiento).Sum(m => m.Monto);
        decimal totalRetiros = movimientos.Where(m => m.Tipo == TipoMovimientoAhorro.Retiro).Sum(m => -m.Monto);
        decimal saldoNeto = movimientos.Sum(m => m.Monto);

        decimal saldoCicloActual = anioConsulta == anioActual
            ? saldoNeto
            : await db.MovimientosAhorro
                .AsNoTracking()
                .Where(m => m.AhorradorId == ahorradorId
                         && m.Fecha >= new DateOnly(anioActual, 1, 1)
                         && m.Fecha <= new DateOnly(anioActual, 12, 31))
                .SumAsync(m => (decimal?)m.Monto, ct) ?? 0m;

        DateOnly fechaCapacidad = anioConsulta == anioActual
            ? DateOnly.FromDateTime(DateTime.Today)
            : finAnio;

        var capacidad = await prestamoService.ObtenerCapacidadAsync(ahorradorId, fechaCapacidad, ct);
        var prestamos = await prestamoService.ListarAsync(ahorradorId, null, ct);

        var movimientosDto = movimientos
            .Select(m => new MovimientoAhorroDto(m.Id, m.Fecha, m.Tipo.ToString().ToUpperInvariant(), m.Monto, m.Concepto))
            .ToList();

        return new EstadoCuentaDto(
            MapearResumen(ahorrador, saldoCicloActual),
            anioConsulta,
            totalOrdinario,
            totalPatronal,
            totalVoluntario,
            totalRendimientos,
            totalRetiros,
            saldoNeto,
            capacidad.MontoMaximoSinAval,
            movimientosDto,
            prestamos);
    }

    private static AhorradorResumenDto MapearResumen(Ahorrador a, decimal saldoCicloActual) =>
        new(
            a.Id,
            a.CajaAhorroId,
            a.CajaAhorro?.Nombre ?? string.Empty,
            a.CajaAhorro?.Empresa?.Nombre ?? string.Empty,
            a.ClaveEmpleado,
            a.Nombre,
            a.Curp,
            a.Clabe,
            a.Estatus.ToString().ToUpperInvariant(),
            a.SalarioBase,
            a.AhorroVoluntarioQuincenal,
            a.FechaIngreso,
            a.FechaBaja,
            saldoCicloActual);
}

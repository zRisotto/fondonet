using FondoNet.Application.Common;
using FondoNet.Application.Prestamos;
using FondoNet.Domain.Entities;
using FondoNet.Domain.Enums;
using FondoNet.Domain.Finance;
using FondoNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FondoNet.Infrastructure.Services;

public class PrestamoService(FondoNetDbContext db) : IPrestamoService
{
    public const decimal PorcentajeTopeSinAval = 0.80m;

    public async Task<CapacidadCreditoDto> ObtenerCapacidadAsync(int ahorradorId, DateOnly? fecha, CancellationToken ct = default)
    {
        var ahorrador = await db.Ahorradores
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == ahorradorId, ct)
            ?? throw new NoEncontradoException($"El ahorrador {ahorradorId} no existe.");

        return await CalcularCapacidadInternaAsync(ahorrador.Id, fecha ?? DateOnly.FromDateTime(DateTime.Today), ct);
    }

    public async Task<SimulacionPrestamoDto> SimularAsync(SolicitudPrestamoRequest solicitud, CancellationToken ct = default)
    {
        var ahorrador = await db.Ahorradores
            .AsNoTracking()
            .Include(a => a.CajaAhorro)
            .FirstOrDefaultAsync(a => a.Id == solicitud.AhorradorId, ct)
            ?? throw new NoEncontradoException($"El ahorrador {solicitud.AhorradorId} no existe.");

        DateOnly fecha = solicitud.FechaSolicitud ?? DateOnly.FromDateTime(DateTime.Today);
        decimal tasa = solicitud.TasaInteresMensual ?? ahorrador.CajaAhorro!.TasaInteresMensual;
        ValidarParametrosCalculo(solicitud.Monto, tasa, solicitud.PlazoQuincenas);

        var capacidad = await CalcularCapacidadInternaAsync(ahorrador.Id, fecha, ct);
        bool requiereAval = solicitud.Monto > capacidad.MontoMaximoSinAval;

        var tabla = AmortizacionCalculator.Generar(solicitud.Monto, tasa, solicitud.PlazoQuincenas, fecha);
        decimal cuotaFija = AmortizacionCalculator.CalcularCuota(solicitud.Monto, tasa, solicitud.PlazoQuincenas);

        var renglones = tabla
            .Select(r => new AmortizacionDto(r.NumeroPago, r.FechaVencimiento, r.SaldoInicial, r.Capital, r.Interes, r.Cuota, r.SaldoFinal, false))
            .ToList();

        return new SimulacionPrestamoDto(
            capacidad,
            solicitud.Monto,
            tasa,
            solicitud.PlazoQuincenas,
            requiereAval,
            cuotaFija,
            tabla.Sum(r => r.Interes),
            renglones);
    }

    public async Task<PrestamoDetalleDto> SolicitarAsync(SolicitudPrestamoRequest solicitud, CancellationToken ct = default)
    {
        var ahorrador = await db.Ahorradores
            .Include(a => a.CajaAhorro)
            .FirstOrDefaultAsync(a => a.Id == solicitud.AhorradorId, ct)
            ?? throw new NoEncontradoException($"El ahorrador {solicitud.AhorradorId} no existe.");

        if (ahorrador.Estatus != EstatusAhorrador.Activo)
            throw new ReglaNegocioException("Solo los ahorradores con estatus ACTIVO pueden solicitar préstamos.");

        DateOnly fecha = solicitud.FechaSolicitud ?? DateOnly.FromDateTime(DateTime.Today);
        decimal tasa = solicitud.TasaInteresMensual ?? ahorrador.CajaAhorro!.TasaInteresMensual;
        ValidarParametrosCalculo(solicitud.Monto, tasa, solicitud.PlazoQuincenas);

        var capacidad = await CalcularCapacidadInternaAsync(ahorrador.Id, fecha, ct);
        bool requiereAval = solicitud.Monto > capacidad.MontoMaximoSinAval;

        Ahorrador? aval = null;
        if (requiereAval || solicitud.AvalAhorradorId.HasValue)
        {
            if (!solicitud.AvalAhorradorId.HasValue)
                throw new ReglaNegocioException(
                    $"El monto ({solicitud.Monto:F2}) excede el tope del 80% ({capacidad.MontoMaximoSinAval:F2}); debe especificar un aval.");

            if (solicitud.AvalAhorradorId.Value == ahorrador.Id)
                throw new ReglaNegocioException("El aval debe ser distinto del solicitante.");

            aval = await db.Ahorradores
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == solicitud.AvalAhorradorId.Value, ct)
                ?? throw new ReglaNegocioException($"El aval con id {solicitud.AvalAhorradorId.Value} no existe.");

            if (aval.CajaAhorroId != ahorrador.CajaAhorroId)
                throw new ReglaNegocioException("El aval debe pertenecer a la misma caja de ahorro que el solicitante.");

            if (aval.Estatus != EstatusAhorrador.Activo)
                throw new ReglaNegocioException("El aval debe tener estatus ACTIVO.");
        }

        var tabla = AmortizacionCalculator.Generar(solicitud.Monto, tasa, solicitud.PlazoQuincenas, fecha);

        var prestamo = new Prestamo
        {
            AhorradorId = ahorrador.Id,
            FechaOtorgamiento = fecha,
            Monto = solicitud.Monto,
            TasaInteresMensual = tasa,
            PlazoQuincenas = solicitud.PlazoQuincenas,
            Estatus = EstatusPrestamo.Activo,
            SaldoInsoluto = solicitud.Monto,
            RequiereAval = requiereAval,
            AvalAhorradorId = aval?.Id,
            Amortizaciones = tabla.Select(r => new Amortizacion
            {
                NumeroPago = r.NumeroPago,
                FechaVencimiento = r.FechaVencimiento,
                SaldoInicial = r.SaldoInicial,
                Capital = r.Capital,
                Interes = r.Interes,
                Cuota = r.Cuota,
                SaldoFinal = r.SaldoFinal,
                Pagada = false
            }).ToList()
        };

        db.Prestamos.Add(prestamo);
        await db.SaveChangesAsync(ct);

        prestamo.Ahorrador = ahorrador;
        prestamo.AvalAhorrador = aval;
        return MapearDetalle(prestamo);
    }

    public async Task<PrestamoDetalleDto> ObtenerAsync(int prestamoId, CancellationToken ct = default)
    {
        var prestamo = await db.Prestamos
            .AsNoTracking()
            .Include(p => p.Ahorrador)
            .Include(p => p.AvalAhorrador)
            .Include(p => p.Amortizaciones)
            .FirstOrDefaultAsync(p => p.Id == prestamoId, ct)
            ?? throw new NoEncontradoException($"El préstamo {prestamoId} no existe.");

        return MapearDetalle(prestamo);
    }

    public async Task<IReadOnlyList<PrestamoDto>> ListarAsync(int? ahorradorId, int? cajaAhorroId, CancellationToken ct = default)
    {
        var query = db.Prestamos
            .AsNoTracking()
            .Include(p => p.Ahorrador)
            .Include(p => p.AvalAhorrador)
            .AsQueryable();

        if (ahorradorId.HasValue)
            query = query.Where(p => p.AhorradorId == ahorradorId.Value);

        if (cajaAhorroId.HasValue)
            query = query.Where(p => p.Ahorrador!.CajaAhorroId == cajaAhorroId.Value);

        var prestamos = await query
            .OrderByDescending(p => p.FechaOtorgamiento)
            .ThenByDescending(p => p.Id)
            .ToListAsync(ct);

        return prestamos.Select(MapearPrestamo).ToList();
    }

    private async Task<CapacidadCreditoDto> CalcularCapacidadInternaAsync(int ahorradorId, DateOnly fecha, CancellationToken ct)
    {
        // El ciclo de ahorro se liquida cada año: el tope se mide en el ciclo vigente [1/ene/año, fecha].
        DateOnly inicioCiclo = new(fecha.Year, 1, 1);

        var movimientos = await db.MovimientosAhorro
            .AsNoTracking()
            .Where(m => m.AhorradorId == ahorradorId
                     && m.Fecha >= inicioCiclo
                     && m.Fecha <= fecha
                     && (m.Tipo == TipoMovimientoAhorro.Ordinario || m.Tipo == TipoMovimientoAhorro.Voluntario))
            .Select(m => new { m.Tipo, m.Monto })
            .ToListAsync(ct);

        decimal ordinario = movimientos.Where(m => m.Tipo == TipoMovimientoAhorro.Ordinario).Sum(m => m.Monto);
        decimal voluntario = movimientos.Where(m => m.Tipo == TipoMovimientoAhorro.Voluntario).Sum(m => m.Monto);
        decimal baseAhorro = ordinario + voluntario;
        decimal tope = Dinero.Redondear(baseAhorro * PorcentajeTopeSinAval);

        return new CapacidadCreditoDto(ahorradorId, fecha, ordinario, voluntario, baseAhorro, tope);
    }

    private static void ValidarParametrosCalculo(decimal monto, decimal tasa, int plazo)
    {
        try
        {
            AmortizacionCalculator.CalcularCuota(monto, tasa, plazo);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new ReglaNegocioException(ex.Message);
        }
    }

    private static PrestamoDto MapearPrestamo(Prestamo p) =>
        new(
            p.Id,
            p.AhorradorId,
            p.Ahorrador?.Nombre ?? string.Empty,
            p.FechaOtorgamiento,
            p.Monto,
            p.TasaInteresMensual,
            p.PlazoQuincenas,
            p.Estatus.ToString().ToUpperInvariant(),
            p.SaldoInsoluto,
            p.RequiereAval,
            p.AvalAhorradorId,
            p.AvalAhorrador?.Nombre,
            p.FechaLiquidacion);

    private static PrestamoDetalleDto MapearDetalle(Prestamo p) =>
        new(
            MapearPrestamo(p),
            p.Amortizaciones
                .OrderBy(a => a.NumeroPago)
                .Select(a => new AmortizacionDto(a.NumeroPago, a.FechaVencimiento, a.SaldoInicial, a.Capital, a.Interes, a.Cuota, a.SaldoFinal, a.Pagada))
                .ToList());
}

namespace FondoNet.Application.Prestamos;

/// <summary>Solicitud o simulación de préstamo.</summary>
public sealed record SolicitudPrestamoRequest(
    int AhorradorId,
    decimal Monto,
    int PlazoQuincenas,
    /// <summary>Si es null se usa la tasa por defecto de la caja (1%–2% mensual).</summary>
    decimal? TasaInteresMensual,
    /// <summary>Si es null se usa la fecha actual.</summary>
    DateOnly? FechaSolicitud,
    /// <summary>Obligatorio cuando el monto excede el 80% del ahorro.</summary>
    int? AvalAhorradorId);

/// <summary>Capacidad de crédito: 80% de ORDINARIO + VOLUNTARIO del ciclo vigente a la fecha.</summary>
public sealed record CapacidadCreditoDto(
    int AhorradorId,
    DateOnly FechaCorte,
    decimal AhorroOrdinario,
    decimal AhorroVoluntario,
    decimal AhorroBase,
    decimal MontoMaximoSinAval);

public sealed record AmortizacionDto(
    int NumeroPago,
    DateOnly FechaVencimiento,
    decimal SaldoInicial,
    decimal Capital,
    decimal Interes,
    decimal Cuota,
    decimal SaldoFinal,
    bool Pagada);

public sealed record SimulacionPrestamoDto(
    CapacidadCreditoDto Capacidad,
    decimal Monto,
    decimal TasaInteresMensual,
    int PlazoQuincenas,
    bool RequiereAval,
    decimal CuotaQuincenal,
    decimal TotalIntereses,
    IReadOnlyList<AmortizacionDto> Amortizaciones);

public sealed record PrestamoDto(
    int Id,
    int AhorradorId,
    string NombreAhorrador,
    DateOnly FechaOtorgamiento,
    decimal Monto,
    decimal TasaInteresMensual,
    int PlazoQuincenas,
    string Estatus,
    decimal SaldoInsoluto,
    bool RequiereAval,
    int? AvalAhorradorId,
    string? NombreAval,
    DateOnly? FechaLiquidacion);

public sealed record PrestamoDetalleDto(PrestamoDto Prestamo, IReadOnlyList<AmortizacionDto> Amortizaciones);

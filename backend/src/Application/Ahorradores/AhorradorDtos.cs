using FondoNet.Application.Prestamos;

namespace FondoNet.Application.Ahorradores;

public sealed record CajaAhorroResumenDto(
    int Id,
    string Nombre,
    string EmpresaNombre,
    decimal TasaInteresMensual,
    decimal PorcentajeAhorro);

public sealed record AhorradorResumenDto(
    int Id,
    int CajaAhorroId,
    string CajaAhorroNombre,
    string EmpresaNombre,
    string ClaveEmpleado,
    string Nombre,
    string Curp,
    string Clabe,
    string Estatus,
    decimal SalarioBase,
    decimal AhorroVoluntarioQuincenal,
    DateOnly FechaIngreso,
    DateOnly? FechaBaja,
    decimal SaldoCicloActual);

public sealed record AvalDisponibleDto(
    int Id,
    string ClaveEmpleado,
    string Nombre);

public sealed record MovimientoAhorroDto(
    int Id,
    DateOnly Fecha,
    string Tipo,
    decimal Monto,
    string Concepto);

public sealed record EstadoCuentaDto(
    AhorradorResumenDto Ahorrador,
    int Anio,
    decimal TotalOrdinario,
    decimal TotalPatronal,
    decimal TotalVoluntario,
    decimal TotalRendimientos,
    decimal TotalRetiros,
    decimal SaldoNeto,
    decimal CapacidadPrestamoSinAval,
    IReadOnlyList<MovimientoAhorroDto> Movimientos,
    IReadOnlyList<PrestamoDto> Prestamos);

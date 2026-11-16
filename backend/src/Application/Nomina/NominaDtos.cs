namespace FondoNet.Application.Nomina;

public enum TipoMovimientoNomina
{
    Alta,
    Baja,
    Incapacidad,
    Licencia
}

/// <summary>Renglón del archivo de nómina: ClaveEmpleado, Fecha, TipoMovimiento, SalarioBase (mensual).</summary>
public sealed record RegistroNomina(int Renglon, string ClaveEmpleado, DateOnly Fecha, TipoMovimientoNomina TipoMovimiento, decimal SalarioBase);

public sealed record ErrorRenglon(int Renglon, string Mensaje);

public sealed record ResultadoParseoNomina(IReadOnlyList<RegistroNomina> Registros, IReadOnlyList<ErrorRenglon> Errores);

public sealed record CambioAhorrador(
    int Renglon,
    int AhorradorId,
    string ClaveEmpleado,
    string TipoMovimiento,
    string EstatusAnterior,
    string EstatusNuevo,
    decimal SalarioBase);

public sealed record ResultadoImportacionNomina(
    int CajaAhorroId,
    string NombreArchivo,
    int RenglonesLeidos,
    int RenglonesAplicados,
    IReadOnlyList<CambioAhorrador> Cambios,
    IReadOnlyList<ErrorRenglon> Errores);

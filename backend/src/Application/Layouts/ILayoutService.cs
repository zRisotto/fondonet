namespace FondoNet.Application.Layouts;

public interface ILayoutService
{
    /// <summary>Descuentos de la quincena (ordinario, voluntario y cuota de préstamo) de los ahorradores activos de la caja.</summary>
    Task<IReadOnlyList<IncidenciaNominaDto>> ObtenerIncidenciasNominaAsync(int cajaAhorroId, DateOnly quincena, CancellationToken ct = default);
    Task<ArchivoGenerado> GenerarIncidenciasNominaAsync(int cajaAhorroId, DateOnly quincena, CancellationToken ct = default);

    /// <summary>Dispersión del RETIRO de cierre (ahorro + rendimientos) de un año ya cerrado.</summary>
    Task<IReadOnlyList<DispersionSpeiDto>> ObtenerDispersionSpeiAsync(int cajaAhorroId, int anio, CancellationToken ct = default);
    Task<ArchivoGenerado> GenerarDispersionSpeiAsync(int cajaAhorroId, int anio, CancellationToken ct = default);
}

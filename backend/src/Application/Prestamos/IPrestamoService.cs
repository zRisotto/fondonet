namespace FondoNet.Application.Prestamos;

public interface IPrestamoService
{
    Task<CapacidadCreditoDto> ObtenerCapacidadAsync(int ahorradorId, DateOnly? fecha, CancellationToken ct = default);

    /// <summary>Calcula tope y tabla sin persistir. No exige aval.</summary>
    Task<SimulacionPrestamoDto> SimularAsync(SolicitudPrestamoRequest solicitud, CancellationToken ct = default);

    /// <summary>Valida tope/aval, guarda el préstamo y su tabla de amortización.</summary>
    Task<PrestamoDetalleDto> SolicitarAsync(SolicitudPrestamoRequest solicitud, CancellationToken ct = default);

    Task<PrestamoDetalleDto> ObtenerAsync(int prestamoId, CancellationToken ct = default);

    Task<IReadOnlyList<PrestamoDto>> ListarAsync(int? ahorradorId, int? cajaAhorroId, CancellationToken ct = default);
}

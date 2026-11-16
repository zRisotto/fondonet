namespace FondoNet.Application.Ahorradores;

public interface IAhorradorService
{
    Task<IReadOnlyList<CajaAhorroResumenDto>> ListarCajasAsync(int? ahorradorId, CancellationToken ct = default);

    Task<IReadOnlyList<AhorradorResumenDto>> ListarAsync(int? cajaAhorroId, CancellationToken ct = default);

    Task<IReadOnlyList<AvalDisponibleDto>> ListarAvalesDisponiblesAsync(int ahorradorId, CancellationToken ct = default);

    Task<EstadoCuentaDto> ObtenerEstadoCuentaAsync(int ahorradorId, int? anio, CancellationToken ct = default);
}

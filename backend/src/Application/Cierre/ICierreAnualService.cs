namespace FondoNet.Application.Cierre;

public interface ICierreAnualService
{
    Task<bool> EstaCerradoAsync(int anio, CancellationToken ct = default);

    /// <summary>
    /// Reparte la bolsa del año por caja y liquida el ciclo (RENDIMIENTO + RETIRO al 31/dic).
    /// Idempotente: si el año ya está cerrado devuelve YaCerrado = true sin escribir.
    /// Con <paramref name="simulacion"/> = true calcula todo sin persistir.
    /// </summary>
    Task<ResultadoCierreAnualDto> EjecutarAsync(int anio, bool simulacion, CancellationToken ct = default);
}

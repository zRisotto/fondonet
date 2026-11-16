using FondoNet.Application.Cierre;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FondoNet.Infrastructure.Services;

/// <summary>
/// Tarea periódica que revisa y cierra únicamente ejercicios anuales ya terminados (año &lt; año actual).
/// </summary>
public class CierreAnualBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<CierreAnualBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(12);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await VerificarYCerrarAniosTerminadosAsync(DateTime.UtcNow.Year, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error en la verificación periódica de cierre anual.");
            }

            await Task.Delay(Intervalo, stoppingToken);
        }
    }

    public async Task VerificarYCerrarAniosTerminadosAsync(int anioActual, CancellationToken ct = default)
    {
        int ultimoAnioTerminado = anioActual - 1;
        using var scope = scopeFactory.CreateScope();
        var cierreService = scope.ServiceProvider.GetRequiredService<ICierreAnualService>();

        if (!await cierreService.EstaCerradoAsync(ultimoAnioTerminado, ct))
        {
            logger.LogInformation("Ejecutando cierre anual automático del ejercicio terminado {Anio}.", ultimoAnioTerminado);
            await cierreService.EjecutarAsync(ultimoAnioTerminado, simulacion: false, ct);
        }
    }
}

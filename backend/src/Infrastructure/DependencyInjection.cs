using FondoNet.Application.Cierre;
using FondoNet.Application.Layouts;
using FondoNet.Application.Nomina;
using FondoNet.Application.Prestamos;
using FondoNet.Infrastructure.Persistence;
using FondoNet.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FondoNet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FondoNetDbContext>(options =>
            options.UseMySql(connectionString, new MariaDbServerVersion(new Version(11, 8, 0))));

        services.AddScoped<IPrestamoService, PrestamoService>();
        services.AddSingleton<INominaParser, NominaParser>();
        services.AddScoped<INominaService, NominaService>();
        services.AddScoped<ICierreAnualService, CierreAnualService>();
        services.AddScoped<ILayoutService, LayoutService>();
        services.AddHostedService<CierreAnualBackgroundService>();

        return services;
    }
}

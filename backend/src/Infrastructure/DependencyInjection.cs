using FondoNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FondoNet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FondoNetDbContext>(options =>
            options.UseMySql(connectionString, new MariaDbServerVersion(new Version(11, 8, 0))));

        return services;
    }
}

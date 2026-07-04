using EchoBoard.Infrastructure.Persistence;
using EchoBoard.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EchoBoard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        services.AddSingleton(settings);
        services.AddDbContext<EchoBoardDbContext>(options => options.UseSqlite(settings.DatabaseConnectionString));

        return services;
    }
}

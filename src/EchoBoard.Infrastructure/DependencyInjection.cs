using EchoBoard.Application.Interfaces;
using EchoBoard.Application.Library;
using EchoBoard.Infrastructure.Files;
using EchoBoard.Infrastructure.Persistence;
using EchoBoard.Infrastructure.Persistence.Repositories;
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
        services.AddScoped<ISoundLibraryRepository, EfSoundLibraryRepository>();
        services.AddScoped<ICategoryRepository, EfCategoryRepository>();
        services.AddScoped<IAudioFileMetadataReader, AudioFileMetadataReader>();
        services.AddScoped<IDatabaseInitializer, EfDatabaseInitializer>();

        return services;
    }
}

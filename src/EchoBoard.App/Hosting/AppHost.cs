using System.Globalization;
using EchoBoard.Application;
using EchoBoard.Application.Interfaces;
using EchoBoard.App.Navigation;
using EchoBoard.App.ViewModels;
using EchoBoard.App.Views;
using EchoBoard.Audio;
using EchoBoard.Infrastructure;
using EchoBoard.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace EchoBoard.App.Hosting;

public static class AppHost
{
    public static IHost Create()
    {
        var settings = AppSettings.CreateDefault();
        Directory.CreateDirectory(settings.AppDataDirectory);
        Directory.CreateDirectory(settings.LogDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(settings.LogDirectory, "echoboard-.log"),
                formatProvider: CultureInfo.InvariantCulture,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true)
            .CreateLogger();

        return Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(services =>
            {
                services.AddSingleton(settings);
                services.AddApplication();
                services.AddAudio();
                services.AddInfrastructure(settings);
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<LibraryViewModel>();
                services.AddTransient<FavoritesViewModel>();
                services.AddTransient<RecentViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<AudioDiagnosticsViewModel>();
                services.AddTransient<MainShellViewModel>();
                services.AddTransient<MainShellPage>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    public static async Task InitializeDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);

        try
        {
            Log.Information("Initializing EchoBoard database.");

            await using var scope = services.CreateAsyncScope();
            var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
            await initializer.InitializeAsync(cancellationToken);

            Log.Information("EchoBoard database initialization completed.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "EchoBoard database initialization failed.");
            throw;
        }
    }
}

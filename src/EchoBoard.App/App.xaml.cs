using EchoBoard.App.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Serilog;

namespace EchoBoard.App;

public partial class App : Microsoft.UI.Xaml.Application
{
    private readonly IHost host;
    private Window? mainWindow;

    public App()
    {
        InitializeComponent();
        host = AppHost.Create();
        UnhandledException += OnUnhandledException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            await host.StartAsync().ConfigureAwait(true);
            await AppHost.InitializeDatabaseAsync(host.Services, CancellationToken.None).ConfigureAwait(true);
            mainWindow = host.Services.GetRequiredService<MainWindow>();
            mainWindow.Activate();
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "EchoBoard failed to start.");
            throw;
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI exception.");
    }
}

using System.Diagnostics;
using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Views;

public sealed partial class SettingsPage : Page
{
    private bool hasLoaded;
    private bool isPageLoaded;
    private readonly Stopwatch meterStopwatch = new();
    private readonly DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(1000.0 / 30.0) };

    public SettingsPage()
    {
        InitializeComponent();
        refreshTimer.Tick += OnRefreshTimerTick;
        Unloaded += OnUnloaded;
    }

    private SettingsViewModel? ViewModel => DataContext as SettingsViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        isPageLoaded = true;
        if (ViewModel is null)
        {
            return;
        }

        if (!hasLoaded)
        {
            hasLoaded = true;
            await ViewModel.LoadAsync(CancellationToken.None);
        }

        if (!isPageLoaded)
        {
            return;
        }

        meterStopwatch.Restart();
        refreshTimer.Start();
    }

    private void OnRefreshTimerTick(object? sender, object e)
    {
        var elapsed = meterStopwatch.Elapsed;
        meterStopwatch.Restart();
        ViewModel?.RefreshMicrophoneSnapshot(elapsed);
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        isPageLoaded = false;
        refreshTimer.Stop();
        meterStopwatch.Stop();
        if (ViewModel is not null)
        {
            try
            {
                await ViewModel.DeactivateAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Failed to deactivate microphone capture: {exception}");
            }
        }
    }
}

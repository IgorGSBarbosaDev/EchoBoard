using EchoBoard.App.Dialogs;
using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Views;

public sealed partial class DashboardPage : Page
{
    private readonly DispatcherTimer liveTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private bool hasLoaded;

    public DashboardPage()
    {
        InitializeComponent();
        liveTimer.Tick += OnLiveTimerTick;
    }

    private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        liveTimer.Start();
        if (hasLoaded || ViewModel is null)
        {
            return;
        }

        hasLoaded = true;
        await ViewModel.LoadAsync(CancellationToken.None);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        liveTimer.Stop();
    }

    private void OnLiveTimerTick(object? sender, object e)
    {
        ViewModel?.RefreshLiveState();
    }

    private async void OnImportClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var paths = await AudioFilePicker.PickMultipleAsync();
        await ViewModel.ImportFilePathsAsync(paths, CancellationToken.None);
    }
}

using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Views;

public sealed partial class MainShellPage : Page
{
    private readonly DispatcherTimer playbackTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    public MainShellPage(MainShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        playbackTimer.Tick += OnPlaybackTimerTick;
    }

    public MainShellViewModel ViewModel { get; }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await ViewModel.LoadAsync(CancellationToken.None);
        playbackTimer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        playbackTimer.Stop();
    }

    private void OnPlaybackTimerTick(object? sender, object e)
    {
        ViewModel.PlaybackBar.Refresh();
    }

    private async void OnPlaybackTimelinePointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Slider slider)
        {
            await ViewModel.PlaybackBar.SeekAsync(slider.Value, CancellationToken.None);
        }
    }
}

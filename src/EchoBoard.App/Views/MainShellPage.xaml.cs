using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.System;

namespace EchoBoard.App.Views;

public sealed partial class MainShellPage : Page
{
    private readonly DispatcherTimer playbackTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private Control? drawerFocusReturnTarget;

    public MainShellPage(MainShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        playbackTimer.Tick += OnPlaybackTimerTick;
        ViewModel.SoundDetails.PropertyChanged += OnSoundDetailsPropertyChanged;
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
        ViewModel.RefreshAudioStatus();
    }

    private async void OnPlaybackTimelinePointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Slider slider)
        {
            await ViewModel.PlaybackBar.SeekAsync(slider.Value, CancellationToken.None);
        }
    }

    private void OnSoundDetailsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SoundDetailsViewModel.IsOpen))
        {
            _ = DispatcherQueue.TryEnqueue(() => AnimateSoundDetails(ViewModel.SoundDetails.IsOpen));
        }
    }

    private void AnimateSoundDetails(bool open)
    {
        var storyboard = new Storyboard();
        var translation = new DoubleAnimation
        {
            To = open ? 0 : 360,
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(translation, SoundDetailsTransform);
        Storyboard.SetTargetProperty(translation, nameof(CompositeTransform.TranslateX));

        var opacity = new DoubleAnimation
        {
            To = open ? 1 : 0,
            Duration = TimeSpan.FromMilliseconds(180)
        };
        Storyboard.SetTarget(opacity, SoundDetailsBackdrop);
        Storyboard.SetTargetProperty(opacity, nameof(UIElement.Opacity));
        storyboard.Children.Add(translation);
        storyboard.Children.Add(opacity);

        if (open)
        {
            drawerFocusReturnTarget = FocusManager.GetFocusedElement(XamlRoot) as Control;
            SoundDetailsOverlay.Visibility = Visibility.Visible;
            SoundDetailsTransform.TranslateX = 360;
            SoundDetailsBackdrop.Opacity = 0;
            storyboard.Completed += (_, _) => SoundDetailsDrawer.FocusInitial();
        }
        else
        {
            storyboard.Completed += (_, _) =>
            {
                SoundDetailsOverlay.Visibility = Visibility.Collapsed;
                drawerFocusReturnTarget?.Focus(FocusState.Programmatic);
            };
        }

        storyboard.Begin();
    }

    private void OnSoundDetailsBackdropClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.SoundDetails.Close();
    }

    private void OnShellKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape && ViewModel.SoundDetails.IsOpen)
        {
            ViewModel.SoundDetails.Close();
            e.Handled = true;
        }
    }
}

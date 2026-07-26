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
    private bool hasLoaded;

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
        playbackTimer.Start();
        if (hasLoaded)
        {
            return;
        }

        hasLoaded = true;
        await ViewModel.LoadAsync(CancellationToken.None);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        playbackTimer.Stop();
    }

    private void OnPlaybackTimerTick(object? sender, object e)
    {
        ViewModel.RefreshPlaybackState();
    }

    private void OnPlaybackTimelinePointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        ViewModel.PlaybackBar.BeginSeek();
    }

    private async void OnPlaybackTimelinePointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Slider slider)
        {
            await ViewModel.PlaybackBar.CommitSeekAsync(slider.Value, CancellationToken.None);
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

        storyboard.Children.Add(translation);

        if (open)
        {
            drawerFocusReturnTarget = FocusManager.GetFocusedElement(XamlRoot) as Control;
            SoundDetailsOverlay.Visibility = Visibility.Visible;
            SoundDetailsTransform.TranslateX = 360;
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

    private void OnSoundDetailsBackdropPressed(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.SoundDetails.Close();
        e.Handled = true;
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

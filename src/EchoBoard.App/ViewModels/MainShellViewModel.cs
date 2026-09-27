using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.App.Navigation;
using EchoBoard.Application.Audio;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.ViewModels;

public sealed partial class MainShellViewModel : ObservableObject
{
    private readonly INavigationService navigationService;
    private readonly GetMicrophoneCaptureSnapshotUseCase getMicrophoneSnapshot;
    private readonly GetAudioRoutingSnapshotUseCase? getAudioRoutingSnapshot;
    private readonly PlaybackCoordinator? playbackCoordinator;
    private readonly Dictionary<ShellRoute, ObservableObject> pages;
    private ShellNavigationItemViewModel selectedNavigationItem;
    private ObservableObject currentPage;
    private bool isNavigationPaneOpen = true;
    private string microphoneStatusLabel = "Mic not configured";
    private string virtualOutputStatusLabel = "Virtual output not configured";

    public MainShellViewModel(
        INavigationService navigationService,
        LibraryViewModel libraryViewModel,
        SettingsViewModel settingsViewModel,
        PlaybackBarViewModel playbackBarViewModel,
        SoundDetailsViewModel soundDetailsViewModel,
        GetMicrophoneCaptureSnapshotUseCase getMicrophoneSnapshot,
        PlaybackCoordinator? playbackCoordinator = null,
        TransientNotificationService? notifications = null,
        GetAudioRoutingSnapshotUseCase? getAudioRoutingSnapshot = null)
    {
        this.navigationService = navigationService;
        this.getMicrophoneSnapshot = getMicrophoneSnapshot;
        this.playbackCoordinator = playbackCoordinator;
        this.getAudioRoutingSnapshot = getAudioRoutingSnapshot;
        PlaybackBar = playbackBarViewModel;
        SoundDetails = soundDetailsViewModel;
        Notifications = notifications ?? new TransientNotificationService();
        pages = new Dictionary<ShellRoute, ObservableObject>
        {
            [ShellRoute.Library] = libraryViewModel,
            [ShellRoute.Settings] = settingsViewModel
        };

        NavigationItems =
        [
            new(ShellRoute.Library, "Library", Symbol.Library, "Open sound library"),
            new(ShellRoute.Settings, "Settings", Symbol.Setting, "Open settings")
        ];

        selectedNavigationItem = NavigationItems.First(item => item.Route == navigationService.CurrentRoute);
        currentPage = pages[selectedNavigationItem.Route];

        NavigateCommand = new RelayCommand<object?>(Navigate);
        ToggleSoundDetailsCommand = new RelayCommand(SoundDetails.Toggle);
        OpenSettingsCommand = new RelayCommand(() => Navigate(ShellRoute.Settings));

        navigationService.RouteChanged += OnRouteChanged;
    }

    public string Title => "EchoBoard";

    public string MicrophoneStatusLabel
    {
        get => microphoneStatusLabel;
        private set => SetProperty(ref microphoneStatusLabel, value);
    }

    public string VirtualOutputStatusLabel
    {
        get => virtualOutputStatusLabel;
        private set => SetProperty(ref virtualOutputStatusLabel, value);
    }

    public PlaybackBarViewModel PlaybackBar { get; }

    public SoundDetailsViewModel SoundDetails { get; }

    public TransientNotificationService Notifications { get; }

    public ObservableCollection<ShellNavigationItemViewModel> NavigationItems { get; }

    public ShellNavigationItemViewModel SelectedNavigationItem
    {
        get => selectedNavigationItem;
        set
        {
            if (SetProperty(ref selectedNavigationItem, value))
            {
                Navigate(value.Route);
            }
        }
    }

    public ObservableObject CurrentPage
    {
        get => currentPage;
        private set => SetProperty(ref currentPage, value);
    }

    public bool IsNavigationPaneOpen
    {
        get => isNavigationPaneOpen;
        set
        {
            if (!SetProperty(ref isNavigationPaneOpen, value))
            {
                return;
            }

            foreach (var item in NavigationItems)
            {
                item.IsLabelVisible = value;
            }
        }
    }

    public IRelayCommand<object?> NavigateCommand { get; }

    public IRelayCommand ToggleSoundDetailsCommand { get; }

    public IRelayCommand OpenSettingsCommand { get; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        await PlaybackBar.LoadAsync(cancellationToken);
        RefreshAudioStatus();
    }

    public void RefreshAudioStatus()
    {
        var snapshot = getMicrophoneSnapshot.Execute();
        MicrophoneStatusLabel = snapshot.SelectedDeviceId is null
            ? "Mic not configured"
            : snapshot.State == MicrophoneCaptureState.Active ? "Mic active" : "Mic ready";
        if (getAudioRoutingSnapshot is not null)
        {
            var routing = getAudioRoutingSnapshot.Execute();
            VirtualOutputStatusLabel = routing.VirtualOutputState switch
            {
                AudioRouteState.Active => "Virtual output active",
                AudioRouteState.Unavailable => "Virtual output unavailable",
                AudioRouteState.Failed => "Virtual output failed",
                _ => "Virtual output not configured"
            };
        }
    }

    public void RefreshPlaybackState()
    {
        if (playbackCoordinator is not null)
        {
            playbackCoordinator.Refresh();
        }
        else
        {
            PlaybackBar.Refresh();
        }
        RefreshAudioStatus();
    }

    private void Navigate(object? parameter)
    {
        switch (parameter)
        {
            case ShellRoute route:
                navigationService.NavigateTo(route);
                break;
            case ShellNavigationItemViewModel item:
                navigationService.NavigateTo(item.Route);
                break;
        }
    }

    private void OnRouteChanged(object? sender, ShellRoute route)
    {
        SelectedNavigationItem = NavigationItems.First(item => item.Route == route);
        CurrentPage = pages[route];
    }

}

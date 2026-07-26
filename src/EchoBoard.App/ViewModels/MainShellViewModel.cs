using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.App.Appearance;
using EchoBoard.App.Navigation;
using EchoBoard.Application.Appearance;
using EchoBoard.Application.Audio;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.ViewModels;

public sealed partial class MainShellViewModel : ObservableObject
{
    private readonly INavigationService navigationService;
    private readonly LoadAppearanceSettingsUseCase loadAppearanceSettings;
    private readonly SaveAppearanceSettingsUseCase saveAppearanceSettings;
    private readonly IAppearanceResourceManager appearanceResourceManager;
    private readonly GetMicrophoneCaptureSnapshotUseCase getMicrophoneSnapshot;
    private readonly GetAudioRoutingSnapshotUseCase? getAudioRoutingSnapshot;
    private readonly PlaybackCoordinator? playbackCoordinator;
    private readonly Dictionary<ShellRoute, ObservableObject> pages;
    private ShellNavigationItemViewModel selectedNavigationItem;
    private ObservableObject currentPage;
    private ElementTheme requestedTheme = ElementTheme.Dark;
    private string selectedThemeLabel = "Dark theme";
    private string selectedAccentPalette = AppearancePalettes.Blue;
    private bool isNavigationPaneOpen = true;
    private string microphoneStatusLabel = "Mic not configured";
    private string virtualOutputStatusLabel = "Virtual output not configured";

    public MainShellViewModel(
        INavigationService navigationService,
        DashboardViewModel dashboardViewModel,
        LibraryViewModel libraryViewModel,
        FavoritesViewModel favoritesViewModel,
        RecentViewModel recentViewModel,
        SettingsViewModel settingsViewModel,
        AudioDiagnosticsViewModel audioDiagnosticsViewModel,
        PlaybackBarViewModel playbackBarViewModel,
        SoundDetailsViewModel soundDetailsViewModel,
        GetMicrophoneCaptureSnapshotUseCase getMicrophoneSnapshot,
        LoadAppearanceSettingsUseCase loadAppearanceSettings,
        SaveAppearanceSettingsUseCase saveAppearanceSettings,
        IAppearanceResourceManager appearanceResourceManager,
        PlaybackCoordinator? playbackCoordinator = null,
        TransientNotificationService? notifications = null,
        GetAudioRoutingSnapshotUseCase? getAudioRoutingSnapshot = null)
    {
        this.navigationService = navigationService;
        this.loadAppearanceSettings = loadAppearanceSettings;
        this.saveAppearanceSettings = saveAppearanceSettings;
        this.appearanceResourceManager = appearanceResourceManager;
        this.getMicrophoneSnapshot = getMicrophoneSnapshot;
        this.playbackCoordinator = playbackCoordinator;
        this.getAudioRoutingSnapshot = getAudioRoutingSnapshot;
        PlaybackBar = playbackBarViewModel;
        SoundDetails = soundDetailsViewModel;
        Notifications = notifications ?? new TransientNotificationService();
        pages = new Dictionary<ShellRoute, ObservableObject>
        {
            [ShellRoute.Dashboard] = dashboardViewModel,
            [ShellRoute.Library] = libraryViewModel,
            [ShellRoute.Favorites] = favoritesViewModel,
            [ShellRoute.Recent] = recentViewModel,
            [ShellRoute.Settings] = settingsViewModel,
            [ShellRoute.AudioDiagnostics] = audioDiagnosticsViewModel
        };

        NavigationItems =
        [
            new(ShellRoute.Dashboard, "Dashboard", Symbol.Home, "Open dashboard"),
            new(ShellRoute.Library, "Library", Symbol.Library, "Open sound library"),
            new(ShellRoute.Favorites, "Favorites", Symbol.Favorite, "Open favorite sounds"),
            new(ShellRoute.Recent, "Recent", Symbol.Clock, "Open recent sounds"),
            new(ShellRoute.Settings, "Settings", Symbol.Setting, "Open settings"),
            new(ShellRoute.AudioDiagnostics, "Audio Diagnostics", Symbol.Repair, "Open audio diagnostics")
        ];

        selectedNavigationItem = NavigationItems.First(item => item.Route == navigationService.CurrentRoute);
        currentPage = pages[selectedNavigationItem.Route];

        NavigateCommand = new RelayCommand<object?>(Navigate);
        ToggleThemeCommand = new AsyncRelayCommand(ToggleThemeAsync);
        ToggleSoundDetailsCommand = new RelayCommand(SoundDetails.Toggle);
        OpenSettingsCommand = new RelayCommand(() => Navigate(ShellRoute.Settings));
        ChangeThemeCommand = new AsyncRelayCommand<string>(ChangeThemeAsync);
        ChangeAccentPaletteCommand = new AsyncRelayCommand<string>(ChangeAccentPaletteAsync);

        navigationService.RouteChanged += OnRouteChanged;
    }

    public string Title => "EchoBoard";

    public string SearchPlaceholder => "Search sounds";

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

    public ElementTheme RequestedTheme
    {
        get => requestedTheme;
        private set => SetProperty(ref requestedTheme, value);
    }

    public string SelectedThemeLabel
    {
        get => selectedThemeLabel;
        private set => SetProperty(ref selectedThemeLabel, value);
    }

    public string ThemeIconGlyph => RequestedTheme == ElementTheme.Dark ? "\uE706" : "\uE708";

    public string ThemeToggleToolTip => RequestedTheme == ElementTheme.Dark
        ? "Switch to light theme"
        : "Switch to dark theme";

    public string SelectedAccentPalette
    {
        get => selectedAccentPalette;
        private set => SetProperty(ref selectedAccentPalette, value);
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

    public IAsyncRelayCommand ToggleThemeCommand { get; }

    public IRelayCommand ToggleSoundDetailsCommand { get; }

    public IRelayCommand OpenSettingsCommand { get; }

    public IAsyncRelayCommand<string> ChangeThemeCommand { get; }

    public IAsyncRelayCommand<string> ChangeAccentPaletteCommand { get; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        await PlaybackBar.LoadAsync(cancellationToken);
        var appearance = await loadAppearanceSettings.ExecuteAsync(cancellationToken);
        ApplyAppearance(appearance.Theme, appearance.AccentPalette);
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

    private async Task ToggleThemeAsync(CancellationToken cancellationToken)
    {
        var theme = RequestedTheme == ElementTheme.Dark ? AppearanceThemes.Light : AppearanceThemes.Dark;
        ApplyAppearance(theme, SelectedAccentPalette);
        await SaveAppearanceAsync(cancellationToken);
    }

    private async Task ChangeThemeAsync(string? themeName, CancellationToken cancellationToken)
    {
        ApplyAppearance(AppearanceThemes.Normalize(themeName), SelectedAccentPalette);
        await SaveAppearanceAsync(cancellationToken);
    }

    private async Task ChangeAccentPaletteAsync(string? palette, CancellationToken cancellationToken)
    {
        ApplyAppearance(CurrentThemeName(), AppearancePalettes.Normalize(palette));
        await SaveAppearanceAsync(cancellationToken);
    }

    private void OnRouteChanged(object? sender, ShellRoute route)
    {
        SelectedNavigationItem = NavigationItems.First(item => item.Route == route);
        CurrentPage = pages[route];
    }

    private void UpdateSelectedThemeLabel()
    {
        SelectedThemeLabel = RequestedTheme switch
        {
            ElementTheme.Light => "Light theme",
            ElementTheme.Dark => "Dark theme",
            _ => "Dark theme"
        };

        OnPropertyChanged(nameof(ThemeIconGlyph));
        OnPropertyChanged(nameof(ThemeToggleToolTip));
    }

    private void ApplyAppearance(string theme, string palette)
    {
        RequestedTheme = AppearanceThemes.Normalize(theme) == AppearanceThemes.Light
            ? ElementTheme.Light
            : ElementTheme.Dark;
        SelectedAccentPalette = AppearancePalettes.Normalize(palette);
        UpdateSelectedThemeLabel();
        appearanceResourceManager.Apply(SelectedAccentPalette, RequestedTheme);
    }

    private Task SaveAppearanceAsync(CancellationToken cancellationToken)
    {
        return saveAppearanceSettings.ExecuteAsync(
            new AppearanceSettingsDto(CurrentThemeName(), SelectedAccentPalette),
            cancellationToken);
    }

    private string CurrentThemeName()
    {
        return RequestedTheme == ElementTheme.Light ? AppearanceThemes.Light : AppearanceThemes.Dark;
    }
}

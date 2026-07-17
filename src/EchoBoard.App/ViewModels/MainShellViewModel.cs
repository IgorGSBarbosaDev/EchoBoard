using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.App.Appearance;
using EchoBoard.App.Controls;
using EchoBoard.App.Navigation;
using EchoBoard.Application.Appearance;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.ViewModels;

public sealed partial class MainShellViewModel : ObservableObject
{
    private readonly INavigationService navigationService;
    private readonly LoadAppearanceSettingsUseCase loadAppearanceSettings;
    private readonly SaveAppearanceSettingsUseCase saveAppearanceSettings;
    private readonly IAppearanceResourceManager appearanceResourceManager;
    private readonly Dictionary<ShellRoute, ObservableObject> pages;
    private ShellNavigationItemViewModel selectedNavigationItem;
    private ObservableObject currentPage;
    private ElementTheme requestedTheme = ElementTheme.Dark;
    private string selectedThemeLabel = "Dark theme";
    private string selectedAccentPalette = AppearancePalettes.Blue;
    private bool isNavigationPaneOpen = true;
    private bool isContextPanelOpen = true;

    public MainShellViewModel(
        INavigationService navigationService,
        DashboardViewModel dashboardViewModel,
        LibraryViewModel libraryViewModel,
        FavoritesViewModel favoritesViewModel,
        RecentViewModel recentViewModel,
        SettingsViewModel settingsViewModel,
        AudioDiagnosticsViewModel audioDiagnosticsViewModel,
        PlaybackBarViewModel playbackBarViewModel,
        LoadAppearanceSettingsUseCase loadAppearanceSettings,
        SaveAppearanceSettingsUseCase saveAppearanceSettings,
        IAppearanceResourceManager appearanceResourceManager)
    {
        this.navigationService = navigationService;
        this.loadAppearanceSettings = loadAppearanceSettings;
        this.saveAppearanceSettings = saveAppearanceSettings;
        this.appearanceResourceManager = appearanceResourceManager;
        PlaybackBar = playbackBarViewModel;
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
        ToggleContextPanelCommand = new RelayCommand(ToggleContextPanel);
        OpenSettingsCommand = new RelayCommand(() => Navigate(ShellRoute.Settings));
        ChangeThemeCommand = new AsyncRelayCommand<string>(ChangeThemeAsync);
        ChangeAccentPaletteCommand = new AsyncRelayCommand<string>(ChangeAccentPaletteAsync);

        ContextSound = new(
            "BRB bumper",
            "Mock queue item",
            "0:07",
            "Ctrl+B",
            "Stream",
            null,
            IsPlaying: true,
            IsFavorite: true,
            IsCompact: true);

        ContextDevices =
        [
            new("Mic", "HyperX SoloCast", Symbol.Microphone, DeviceStatusKind.Connected),
            new("Output", "Virtual output not set", Symbol.Audio, DeviceStatusKind.Warning)
        ];

        MixerVolumes =
        [
            new("Mic", Symbol.Microphone, 75),
            new("Effects", Symbol.Audio, 80),
            new("Monitor", Symbol.Volume, 60),
            new("Output", Symbol.Volume, 0)
        ];

        MixerMeters =
        [
            new("Mic", 0.54, AudioLevelMeterVariant.Microphone),
            new("Effects", 0.35, AudioLevelMeterVariant.Effects),
            new("Output", 0.0, AudioLevelMeterVariant.VirtualOutput, "Idle")
        ];

        PreviewToast = new(
            ToastNotificationKind.Warning,
            "Virtual output not configured",
            "This is a non-blocking mock notification preview.");

        navigationService.RouteChanged += OnRouteChanged;
    }

    public string Title => "EchoBoard";

    public string SearchPlaceholder => "Search sounds";

    public string MicrophoneStatusLabel => "Mic ready";

    public string VirtualOutputStatusLabel => "Virtual output not set";

    public string CurrentSoundTitle => "No sound selected";

    public string CurrentSoundSubtitle => "Playback controls are reserved for the audio phase.";

    public string VirtualOutputStateLabel => "Output idle";

    public PlaybackBarViewModel PlaybackBar { get; }

    public string ContextPanelTitle => "Session Panel";

    public string ContextPanelSubtitle => "Queue, active sounds, device state, and sound details will appear here.";

    public SoundCardPreviewModel ContextSound { get; }

    public IReadOnlyList<DevicePreviewModel> ContextDevices { get; }

    public IReadOnlyList<VolumePreviewModel> MixerVolumes { get; }

    public IReadOnlyList<AudioMeterPreviewModel> MixerMeters { get; }

    public ToastPreviewModel PreviewToast { get; }

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

    public bool IsContextPanelOpen
    {
        get => isContextPanelOpen;
        private set
        {
            if (SetProperty(ref isContextPanelOpen, value))
            {
                OnPropertyChanged(nameof(ContextPanelColumnWidth));
                OnPropertyChanged(nameof(ContextPanelVisibility));
                OnPropertyChanged(nameof(ContextPanelExpandButtonVisibility));
            }
        }
    }

    public GridLength ContextPanelColumnWidth => IsContextPanelOpen ? new GridLength(300) : new GridLength(0);

    public Visibility ContextPanelVisibility => IsContextPanelOpen ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ContextPanelExpandButtonVisibility => IsContextPanelOpen ? Visibility.Collapsed : Visibility.Visible;

    public IRelayCommand<object?> NavigateCommand { get; }

    public IAsyncRelayCommand ToggleThemeCommand { get; }

    public IRelayCommand ToggleContextPanelCommand { get; }

    public IRelayCommand OpenSettingsCommand { get; }

    public IAsyncRelayCommand<string> ChangeThemeCommand { get; }

    public IAsyncRelayCommand<string> ChangeAccentPaletteCommand { get; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        await PlaybackBar.LoadAsync(cancellationToken);
        var appearance = await loadAppearanceSettings.ExecuteAsync(cancellationToken);
        ApplyAppearance(appearance.Theme, appearance.AccentPalette);
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

    private void ToggleContextPanel()
    {
        IsContextPanelOpen = !IsContextPanelOpen;
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

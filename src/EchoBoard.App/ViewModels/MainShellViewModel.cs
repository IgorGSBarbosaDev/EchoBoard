using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.App.Navigation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.ViewModels;

public sealed partial class MainShellViewModel : ObservableObject
{
    private readonly INavigationService navigationService;
    private readonly Dictionary<ShellRoute, ObservableObject> pages;
    private ShellNavigationItemViewModel selectedNavigationItem;
    private ObservableObject currentPage;
    private ElementTheme requestedTheme = ElementTheme.Default;
    private string selectedThemeLabel = "System theme";
    private bool isContextPanelOpen = true;

    public MainShellViewModel(
        INavigationService navigationService,
        DashboardViewModel dashboardViewModel,
        LibraryViewModel libraryViewModel,
        FavoritesViewModel favoritesViewModel,
        RecentViewModel recentViewModel,
        SettingsViewModel settingsViewModel,
        AudioDiagnosticsViewModel audioDiagnosticsViewModel)
    {
        this.navigationService = navigationService;
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
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        ToggleContextPanelCommand = new RelayCommand(ToggleContextPanel);
        OpenSettingsCommand = new RelayCommand(() => Navigate(ShellRoute.Settings));
        ChangeThemeCommand = new RelayCommand<string>(ChangeTheme);

        navigationService.RouteChanged += OnRouteChanged;
    }

    public string Title => "EchoBoard";

    public string SearchPlaceholder => "Search sounds";

    public string MicrophoneStatusLabel => "Mic ready";

    public string VirtualOutputStatusLabel => "Virtual output not set";

    public string CurrentSoundTitle => "No sound selected";

    public string CurrentSoundSubtitle => "Playback controls are reserved for the audio phase.";

    public string VirtualOutputStateLabel => "Output idle";

    public string ContextPanelTitle => "Session Panel";

    public string ContextPanelSubtitle => "Queue, active sounds, device state, and sound details will appear here.";

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

    public IRelayCommand ToggleThemeCommand { get; }

    public IRelayCommand ToggleContextPanelCommand { get; }

    public IRelayCommand OpenSettingsCommand { get; }

    public IRelayCommand<string> ChangeThemeCommand { get; }

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

    private void ToggleTheme()
    {
        RequestedTheme = RequestedTheme switch
        {
            ElementTheme.Default => ElementTheme.Dark,
            ElementTheme.Dark => ElementTheme.Light,
            _ => ElementTheme.Default
        };

        UpdateSelectedThemeLabel();
    }

    private void ToggleContextPanel()
    {
        IsContextPanelOpen = !IsContextPanelOpen;
    }

    private void ChangeTheme(string? themeName)
    {
        RequestedTheme = themeName switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        UpdateSelectedThemeLabel();
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
            _ => "System theme"
        };
    }
}

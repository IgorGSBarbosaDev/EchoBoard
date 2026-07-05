using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace EchoBoard.App.ViewModels;

public sealed partial class MainShellViewModel : ObservableObject
{
    private string title = "EchoBoard";
    private ElementTheme requestedTheme = ElementTheme.Default;
    private string selectedThemeLabel = "System theme";

    public MainShellViewModel()
    {
        ChangeThemeCommand = new RelayCommand<string>(ChangeTheme);
    }

    public string Title
    {
        get => title;
        set => SetProperty(ref title, value);
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

    public IRelayCommand<string> ChangeThemeCommand { get; }

    private void ChangeTheme(string? themeName)
    {
        RequestedTheme = themeName switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        SelectedThemeLabel = RequestedTheme switch
        {
            ElementTheme.Light => "Light theme",
            ElementTheme.Dark => "Dark theme",
            _ => "System theme"
        };
    }
}

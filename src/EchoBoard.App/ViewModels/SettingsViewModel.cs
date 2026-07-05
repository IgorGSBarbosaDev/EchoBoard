using CommunityToolkit.Mvvm.ComponentModel;

namespace EchoBoard.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    public string Title => "Settings";

    public string Subtitle => "Application preferences and daily-use behavior will be configured here.";

    public string EmptyStateTitle => "Settings shell ready";

    public string EmptyStateMessage => "Theme, startup, compact mode, and reset controls will be added in later phases.";
}

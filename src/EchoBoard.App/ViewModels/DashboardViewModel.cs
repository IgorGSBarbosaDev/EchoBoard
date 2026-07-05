using CommunityToolkit.Mvvm.ComponentModel;

namespace EchoBoard.App.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    public string Title => "Dashboard";

    public string Subtitle => "Overview of the soundboard workspace before live audio features are connected.";

    public string EmptyStateTitle => "Ready for your first session";

    public string EmptyStateMessage => "Future imports, device health, and playback activity will appear here.";
}

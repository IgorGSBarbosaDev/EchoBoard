using CommunityToolkit.Mvvm.ComponentModel;

namespace EchoBoard.App.ViewModels;

public sealed class RecentViewModel : ObservableObject
{
    public string Title => "Recent";

    public string Subtitle => "Playback history placeholder for recently triggered sounds.";

    public string EmptyStateTitle => "Nothing played recently";

    public string EmptyStateMessage => "Recently played sounds will appear here after playback is implemented.";
}

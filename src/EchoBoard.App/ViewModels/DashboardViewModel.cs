using CommunityToolkit.Mvvm.ComponentModel;
using EchoBoard.App.Controls;

namespace EchoBoard.App.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    public DashboardViewModel()
    {
        PreviewSounds =
        [
            new(
                "Intro sting",
                "Ready for stream start",
                "0:04",
                "Ctrl+1",
                "Stream",
                null,
                IsSelected: true),
            new(
                "Level up",
                "Favorite short effect",
                "0:02",
                "Alt+L",
                "Games",
                null,
                IsPlaying: true,
                IsFavorite: true),
            new(
                "Air horn",
                "Compact card preview",
                "0:01",
                "",
                "Memes",
                null,
                IsCompact: true)
        ];

        PreviewToast = new(
            ToastNotificationKind.Info,
            "Mock preview only",
            "Components are wired with sample data until library and audio phases are implemented.");

        PreviewEmptyState = new(
            Microsoft.UI.Xaml.Controls.Symbol.Audio,
            "No active session",
            "Imported sounds, playback, and device activity will appear here in later phases.",
            "Import sounds",
            "View diagnostics");
    }

    public string Title => "Dashboard";

    public string Subtitle => "Overview of the soundboard workspace before live audio features are connected.";

    public string EmptyStateTitle => "Ready for your first session";

    public string EmptyStateMessage => "Future imports, device health, and playback activity will appear here.";

    public IReadOnlyList<SoundCardPreviewModel> PreviewSounds { get; }

    public ToastPreviewModel PreviewToast { get; }

    public EmptyStatePreviewModel PreviewEmptyState { get; }
}

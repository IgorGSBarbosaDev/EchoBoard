using CommunityToolkit.Mvvm.ComponentModel;

namespace EchoBoard.App.ViewModels;

public sealed class LibraryViewModel : ObservableObject
{
    public LibraryViewModel()
    {
        PreviewCategories =
        [
            new("Favorites", "12", Microsoft.UI.Xaml.Controls.Symbol.Favorite, null, IsSelected: true),
            new("Stream", "8", Microsoft.UI.Xaml.Controls.Symbol.Video, null),
            new("Games", "18", Microsoft.UI.Xaml.Controls.Symbol.Play, null),
            new("Unavailable", "3", Microsoft.UI.Xaml.Controls.Symbol.BlockContact, null, IsEnabled: false)
        ];

        PreviewSounds =
        [
            new(
                "Victory cue",
                "Games / short",
                "0:03",
                "Ctrl+V",
                "Games",
                null,
                IsSelected: true,
                IsFavorite: true),
            new(
                "BRB bumper",
                "Stream transition",
                "0:07",
                "Ctrl+B",
                "Stream",
                null,
                IsPlaying: true),
            new(
                "Error beep",
                "Disabled file preview",
                "0:01",
                "",
                "Alerts",
                null,
                IsEnabled: false),
            new(
                "Nice shot",
                "Compact layout preview",
                "0:02",
                "Alt+N",
                "Games",
                null,
                IsCompact: true)
        ];
    }

    public string Title => "Library";

    public string Subtitle => "Sound organization space prepared for imports, categories, and search.";

    public string EmptyStateTitle => "No sounds imported";

    public string EmptyStateMessage => "Imported MP3 and WAV files will be organized here in a later phase.";

    public IReadOnlyList<CategoryPreviewModel> PreviewCategories { get; }

    public IReadOnlyList<SoundCardPreviewModel> PreviewSounds { get; }
}

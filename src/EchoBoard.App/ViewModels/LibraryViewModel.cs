using CommunityToolkit.Mvvm.ComponentModel;

namespace EchoBoard.App.ViewModels;

public sealed class LibraryViewModel : ObservableObject
{
    public string Title => "Library";

    public string Subtitle => "Sound organization space prepared for imports, categories, and search.";

    public string EmptyStateTitle => "No sounds imported";

    public string EmptyStateMessage => "Imported MP3 and WAV files will be organized here in a later phase.";
}

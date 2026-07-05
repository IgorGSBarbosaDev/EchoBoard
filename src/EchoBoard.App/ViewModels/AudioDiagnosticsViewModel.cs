using CommunityToolkit.Mvvm.ComponentModel;

namespace EchoBoard.App.ViewModels;

public sealed class AudioDiagnosticsViewModel : ObservableObject
{
    public string Title => "Audio Diagnostics";

    public string Subtitle => "Device state and routing diagnostics placeholder.";

    public string EmptyStateTitle => "No live audio engine yet";

    public string EmptyStateMessage => "Microphone, monitor, virtual output, format, and engine status will appear here.";
}

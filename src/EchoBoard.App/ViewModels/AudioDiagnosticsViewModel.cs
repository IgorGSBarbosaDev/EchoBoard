using CommunityToolkit.Mvvm.ComponentModel;
using EchoBoard.App.Controls;

namespace EchoBoard.App.ViewModels;

public sealed class AudioDiagnosticsViewModel : ObservableObject
{
    public AudioDiagnosticsViewModel()
    {
        PreviewDevices =
        [
            new("Microphone", "HyperX SoloCast", Microsoft.UI.Xaml.Controls.Symbol.Microphone, DeviceStatusKind.Connected),
            new("Monitor", "Headphones", Microsoft.UI.Xaml.Controls.Symbol.Volume, DeviceStatusKind.Warning),
            new("Virtual output", "VB-CABLE input", Microsoft.UI.Xaml.Controls.Symbol.Audio, DeviceStatusKind.Unavailable),
            new("Audio engine", "Waiting for configuration", Microsoft.UI.Xaml.Controls.Symbol.Sync, DeviceStatusKind.Loading)
        ];

        PreviewMeters =
        [
            new("Mic", 0.64, AudioLevelMeterVariant.Microphone),
            new("Effects", 0.42, AudioLevelMeterVariant.Effects),
            new("Monitor", 0.28, AudioLevelMeterVariant.Monitor),
            new("Virtual output", 0.0, AudioLevelMeterVariant.VirtualOutput, "Idle")
        ];
    }

    public string Title => "Audio Diagnostics";

    public string Subtitle => "Device state and routing diagnostics placeholder.";

    public string EmptyStateTitle => "No live audio engine yet";

    public string EmptyStateMessage => "Microphone, monitor, virtual output, format, and engine status will appear here.";

    public IReadOnlyList<DevicePreviewModel> PreviewDevices { get; }

    public IReadOnlyList<AudioMeterPreviewModel> PreviewMeters { get; }
}

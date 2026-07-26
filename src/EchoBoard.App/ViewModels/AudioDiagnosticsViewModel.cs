using CommunityToolkit.Mvvm.ComponentModel;
using EchoBoard.App.Controls;
using EchoBoard.Application.Audio;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.ViewModels;

public sealed class AudioDiagnosticsViewModel : ObservableObject
{
    private readonly GetMicrophoneCaptureSnapshotUseCase getMicrophoneCaptureSnapshot;
    private readonly GetAudioRoutingSnapshotUseCase? getAudioRoutingSnapshot;
    private DevicePreviewModel microphoneDevice = new("Microphone", "No microphone selected", Symbol.Microphone, DeviceStatusKind.Unavailable);
    private DevicePreviewModel monitorDevice = new("Monitor", "Windows default output", Symbol.Audio, DeviceStatusKind.Unavailable);
    private DevicePreviewModel virtualOutputDevice = new("Virtual output", "Not configured", Symbol.Audio, DeviceStatusKind.Unavailable);
    private AudioMeterPreviewModel microphoneMeter = new("Mic", 0, AudioLevelMeterVariant.Microphone, "Idle");
    private string formatText = "No active microphone format";
    private string lastErrorText = "No microphone errors";
    private string mixerStateText = "Mixer stopped";
    private string monitorRouteText = "Monitor stopped";
    private string virtualOutputRouteText = "Virtual output not configured";

    public AudioDiagnosticsViewModel(
        GetMicrophoneCaptureSnapshotUseCase getMicrophoneCaptureSnapshot,
        GetAudioRoutingSnapshotUseCase? getAudioRoutingSnapshot = null)
    {
        this.getMicrophoneCaptureSnapshot = getMicrophoneCaptureSnapshot;
        this.getAudioRoutingSnapshot = getAudioRoutingSnapshot;
        Refresh();
    }

    public string Title => "Audio Diagnostics";

    public string Subtitle => "Live state of the microphone, mixer, local monitor, and virtual output.";

    public string EmptyStateTitle => "Microphone capture is stopped";

    public string EmptyStateMessage => "Select a microphone in Settings and start capture to see input level.";

    public DevicePreviewModel MicrophoneDevice
    {
        get => microphoneDevice;
        private set => SetProperty(ref microphoneDevice, value);
    }

    public AudioMeterPreviewModel MicrophoneMeter
    {
        get => microphoneMeter;
        private set => SetProperty(ref microphoneMeter, value);
    }

    public DevicePreviewModel MonitorDevice
    {
        get => monitorDevice;
        private set => SetProperty(ref monitorDevice, value);
    }

    public DevicePreviewModel VirtualOutputDevice
    {
        get => virtualOutputDevice;
        private set => SetProperty(ref virtualOutputDevice, value);
    }

    public string MixerStateText
    {
        get => mixerStateText;
        private set => SetProperty(ref mixerStateText, value);
    }

    public string FormatText
    {
        get => formatText;
        private set => SetProperty(ref formatText, value);
    }

    public string LastErrorText
    {
        get => lastErrorText;
        private set => SetProperty(ref lastErrorText, value);
    }

    public string MonitorRouteText
    {
        get => monitorRouteText;
        private set => SetProperty(ref monitorRouteText, value);
    }

    public string VirtualOutputRouteText
    {
        get => virtualOutputRouteText;
        private set => SetProperty(ref virtualOutputRouteText, value);
    }

    public IReadOnlyList<DevicePreviewModel> PreviewDevices => [MicrophoneDevice, MonitorDevice, VirtualOutputDevice];

    public IReadOnlyList<AudioMeterPreviewModel> PreviewMeters => [MicrophoneMeter];

    public void Refresh()
    {
        Apply(getMicrophoneCaptureSnapshot.Execute());
        if (getAudioRoutingSnapshot is not null)
        {
            ApplyRouting(getAudioRoutingSnapshot.Execute());
        }
    }

    private void Apply(MicrophoneCaptureSnapshot snapshot)
    {
        MicrophoneDevice = new DevicePreviewModel(
            "Microphone",
            string.IsNullOrWhiteSpace(snapshot.SelectedDeviceName) ? "No microphone selected" : snapshot.SelectedDeviceName,
            Symbol.Microphone,
            snapshot.State switch
            {
                MicrophoneCaptureState.Active => DeviceStatusKind.Connected,
                MicrophoneCaptureState.Starting => DeviceStatusKind.Loading,
                MicrophoneCaptureState.Unavailable => DeviceStatusKind.Unavailable,
                MicrophoneCaptureState.Failed => DeviceStatusKind.Warning,
                _ => DeviceStatusKind.Disconnected
            });

        MicrophoneMeter = new AudioMeterPreviewModel(
            "Mic",
            snapshot.IsMuted ? 0 : snapshot.Level,
            AudioLevelMeterVariant.Microphone,
            snapshot.IsMuted ? "Muted" : snapshot.State == MicrophoneCaptureState.Active ? $"{snapshot.Level:P0}" : "Idle");
        FormatText = snapshot.Format?.DisplayText ?? "No active microphone format";
        LastErrorText = snapshot.ErrorMessage ?? snapshot.StatusMessage;
        OnPropertyChanged(nameof(PreviewDevices));
        OnPropertyChanged(nameof(PreviewMeters));
    }

    private void ApplyRouting(AudioRoutingSnapshot snapshot)
    {
        MonitorDevice = new DevicePreviewModel(
            "Local monitor",
            snapshot.MonitorDeviceName ?? "Windows default output",
            Symbol.Audio,
            ToDeviceStatus(snapshot.MonitorState));
        VirtualOutputDevice = new DevicePreviewModel(
            "Virtual output",
            snapshot.VirtualOutputDeviceName ?? "No external cable selected",
            Symbol.Audio,
            ToDeviceStatus(snapshot.VirtualOutputState));
        MixerStateText = $"{snapshot.EngineState} · {snapshot.Format.DisplayText}";
        MonitorRouteText = snapshot.MonitorState == AudioRouteState.Active
            ? $"Monitor format: {snapshot.MonitorFormat?.DisplayText ?? snapshot.Format.DisplayText}"
            : snapshot.MonitorErrorMessage ?? $"Monitor: {snapshot.MonitorState}";
        VirtualOutputRouteText = snapshot.VirtualOutputState switch
        {
            AudioRouteState.Active =>
                $"Virtual format: {snapshot.VirtualOutputFormat?.DisplayText ?? snapshot.Format.DisplayText}",
            AudioRouteState.Unconfigured =>
                "Install and select a virtual cable; local playback remains available.",
            _ => snapshot.VirtualOutputErrorMessage ?? $"Virtual output: {snapshot.VirtualOutputState}"
        };
        LastErrorText = snapshot.ErrorMessage
                        ?? snapshot.VirtualOutputErrorMessage
                        ?? snapshot.MonitorErrorMessage
                        ?? snapshot.StatusMessage;
        FormatText = snapshot.Format.DisplayText;
        OnPropertyChanged(nameof(PreviewDevices));
    }

    private static DeviceStatusKind ToDeviceStatus(AudioRouteState state)
    {
        return state switch
        {
            AudioRouteState.Active => DeviceStatusKind.Connected,
            AudioRouteState.Starting => DeviceStatusKind.Loading,
            AudioRouteState.Unavailable or AudioRouteState.Unconfigured => DeviceStatusKind.Unavailable,
            AudioRouteState.Failed => DeviceStatusKind.Warning,
            _ => DeviceStatusKind.Disconnected
        };
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using EchoBoard.App.Controls;
using EchoBoard.Application.Audio;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.ViewModels;

public sealed class AudioDiagnosticsViewModel : ObservableObject
{
    private readonly GetMicrophoneCaptureSnapshotUseCase getMicrophoneCaptureSnapshot;
    private DevicePreviewModel microphoneDevice = new("Microphone", "No microphone selected", Symbol.Microphone, DeviceStatusKind.Unavailable);
    private AudioMeterPreviewModel microphoneMeter = new("Mic", 0, AudioLevelMeterVariant.Microphone, "Idle");
    private string formatText = "No active microphone format";
    private string lastErrorText = "No microphone errors";

    public AudioDiagnosticsViewModel(GetMicrophoneCaptureSnapshotUseCase getMicrophoneCaptureSnapshot)
    {
        this.getMicrophoneCaptureSnapshot = getMicrophoneCaptureSnapshot;
        Refresh();
    }

    public string Title => "Audio Diagnostics";

    public string Subtitle => "Live microphone capture state for the future mixer input.";

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

    public IReadOnlyList<DevicePreviewModel> PreviewDevices => [MicrophoneDevice];

    public IReadOnlyList<AudioMeterPreviewModel> PreviewMeters => [MicrophoneMeter];

    public void Refresh()
    {
        Apply(getMicrophoneCaptureSnapshot.Execute());
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
}

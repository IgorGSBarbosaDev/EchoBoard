using EchoBoard.App.ViewModels;
using EchoBoard.Application.Audio;
using FluentAssertions;
using Xunit;

namespace EchoBoard.App.Tests;

public sealed class VirtualRoutingUiTests
{
    [Fact]
    public void OutputOptionsDistinguishVirtualCandidatesAndSavedUnavailableDevices()
    {
        var candidate = AudioOutputDeviceOptionViewModel.From(
            new AudioOutputDeviceDto(
                "cable",
                "CABLE Input",
                false,
                true,
                IsVirtualOutputCandidate: true,
                EndpointFamily: "vb-cable"));
        var unavailable = new AudioOutputDeviceOptionViewModel(
            "missing",
            "VoiceMeeter Input",
            false,
            false,
            IsPersistedUnavailable: true);

        candidate.DisplayName.Should().Be("CABLE Input (Virtual cable)");
        candidate.EndpointFamily.Should().Be("vb-cable");
        unavailable.DisplayName.Should().Be("VoiceMeeter Input (Unavailable)");
    }

    [Fact]
    public void DiagnosticsExposesVirtualRouteFailureAndEndpointFormats()
    {
        var routing = new FakeRoutingEngine
        {
            Snapshot = new AudioRoutingSnapshot(
                AudioRouteState.Active,
                AudioRouteState.Active,
                AudioRouteState.Active,
                AudioRouteState.Failed,
                "Microphone (NVIDIA Broadcast)",
                "Speakers (Realtek Audio)",
                "CABLE Input",
                "Virtual output failed. Local playback remains available.",
                null,
                new AudioStreamFormatDto(48000, 2, 32, "IEEE Float"),
                null,
                "Cable disconnected",
                new AudioStreamFormatDto(48000, 2, 32, "IEEE Float"),
                null)
        };
        var viewModel = new AudioDiagnosticsViewModel(
            new GetMicrophoneCaptureSnapshotUseCase(new FakeMicrophoneController()),
            new GetAudioRoutingSnapshotUseCase(routing));

        viewModel.Refresh();

        viewModel.VirtualOutputRouteText.Should().Be("Cable disconnected");
        viewModel.MonitorRouteText.Should().Contain("48000 Hz");
        viewModel.LastErrorText.Should().Be("Cable disconnected");
    }

    [Fact]
    public void SettingsAndDiagnosticsContainCompactVirtualRouteGuidance()
    {
        var root = FindRepositoryRoot();
        var settings = File.ReadAllText(Path.Combine(root, "src", "EchoBoard.App", "Views", "SettingsPage.xaml"));
        var diagnostics = File.ReadAllText(Path.Combine(root, "src", "EchoBoard.App", "Views", "AudioDiagnosticsPage.xaml"));

        settings.Should().Contain("VirtualOutputWarningVisibility");
        settings.Should().Contain("VirtualOutputWarningText");
        settings.Should().Contain("Select VB-CABLE or VoiceMeeter");
        diagnostics.Should().Contain("VirtualOutputRouteText");
    }

    [Fact]
    public void SettingsAndDiagnosticsUseResponsiveRectangularDeviceCards()
    {
        var root = FindRepositoryRoot();
        var appRoot = Path.Combine(root, "src", "EchoBoard.App");
        var card = File.ReadAllText(Path.Combine(appRoot, "Controls", "DeviceStatusBadge.xaml"));
        var settings = File.ReadAllText(Path.Combine(appRoot, "Views", "SettingsPage.xaml"));
        var diagnostics = File.ReadAllText(Path.Combine(appRoot, "Views", "AudioDiagnosticsPage.xaml"));

        card.Should().Contain("EchoBoardDeviceStatusCardStyle");
        card.Should().Contain("TextWrapping=\"Wrap\"");
        card.Should().NotContain("EchoBoardRadiusPill");
        settings.Should().Contain("RefreshMicrophoneButton");
        settings.Should().Contain("DeviceName=\"{Binding SelectedMicrophoneName}\"");
        diagnostics.Should().Contain("DeviceCardsGrid");
        diagnostics.Should().Contain("MonitorDeviceCard");
        diagnostics.Should().Contain("VirtualOutputDeviceCard");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "EchoBoard.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private sealed class FakeRoutingEngine : IAudioRoutingEngine
    {
        public AudioRoutingSnapshot Snapshot { get; set; } = AudioRoutingSnapshot.Stopped;

        public Task InitializeAsync(AudioRoutingSettingsDto settings, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task ApplySettingsAsync(AudioRoutingSettingsDto settings, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        public AudioRoutingSnapshot GetRoutingSnapshot() => Snapshot;
    }

    private sealed class FakeMicrophoneController : IMicrophoneCaptureController
    {
        public IMicrophonePcmSource? CurrentSource => null;

        public Task<IReadOnlyList<AudioInputDeviceDto>> ListInputDevicesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AudioInputDeviceDto>>([]);

        public Task RestoreSelectionAsync(MicrophoneSettingsDto settings, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SelectDeviceAsync(AudioInputDeviceDto device, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SetGainAsync(double gain, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SetMutedAsync(bool isMuted, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        public MicrophoneCaptureSnapshot GetSnapshot() => new(
            MicrophoneCaptureState.Active,
            "mic",
            "Microphone (NVIDIA Broadcast)",
            0.2,
            1,
            false,
            "Capturing",
            null,
            new AudioStreamFormatDto(48000, 2, 32, "IEEE Float"));
    }
}

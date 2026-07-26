using EchoBoard.Application.Audio;
using FluentAssertions;
using Xunit;

namespace EchoBoard.Application.Tests;

public sealed class AudioRoutingUseCaseTests
{
    [Fact]
    public async Task FirstConfigurationPrefersNvidiaBroadcastAndDefaultMonitor()
    {
        var microphones = new FakeMicrophoneController(
        [
            new AudioInputDeviceDto("default-mic", "Microphone (Realtek Audio)", true, true),
            new AudioInputDeviceDto("broadcast", "Microphone (NVIDIA Broadcast)", false, true, "nvidia-broadcast")
        ]);
        var outputs = new FakeOutputEnumerator(
        [
            new AudioOutputDeviceDto("realtek", "Speakers (Realtek Audio)", true, true),
            new AudioOutputDeviceDto("cable", "CABLE Input", false, true, true, "vb-cable")
        ]);

        var result = await new LoadAudioRoutingSettingsUseCase(
                new FakeSettings(),
                microphones,
                outputs)
            .ExecuteAsync(TestContext.Current.CancellationToken);

        result.InputDeviceId.Should().Be("broadcast");
        result.InputDeviceName.Should().Contain("NVIDIA Broadcast");
        result.MonitorDeviceId.Should().Be("realtek");
        result.VirtualOutputDeviceId.Should().BeNull();
    }

    [Fact]
    public async Task MissingSavedVirtualOutputRemainsPersistedForAutomaticReconnect()
    {
        var settings = new FakeSettings();
        settings.Values[AudioRoutingSettingKeys.VirtualOutputDeviceId] = "missing-cable-id";
        settings.Values[AudioRoutingSettingKeys.VirtualOutputDeviceName] = "CABLE Input";
        var microphones = new FakeMicrophoneController(
        [
            new AudioInputDeviceDto("broadcast", "Microphone (NVIDIA Broadcast)", true, true, "nvidia-broadcast")
        ]);
        var outputs = new FakeOutputEnumerator(
        [
            new AudioOutputDeviceDto("realtek", "Speakers (Realtek Audio)", true, true)
        ]);

        var result = await new LoadAudioRoutingSettingsUseCase(settings, microphones, outputs)
            .ExecuteAsync(TestContext.Current.CancellationToken);

        result.VirtualOutputDeviceId.Should().Be("missing-cable-id");
        result.VirtualOutputDeviceName.Should().Be("CABLE Input");
    }

    private sealed class FakeSettings : IAppSettingRepository
    {
        public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);

        public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
        {
            return Task.FromResult(Values.GetValueOrDefault(key));
        }

        public Task UpsertValueAsync(string key, string value, CancellationToken cancellationToken)
        {
            Values[key] = value;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOutputEnumerator(IReadOnlyList<AudioOutputDeviceDto> devices)
        : IAudioOutputDeviceEnumerator
    {
        public Task<IReadOnlyList<AudioOutputDeviceDto>> ListOutputDevicesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(devices);
        }
    }

    private sealed class FakeMicrophoneController(IReadOnlyList<AudioInputDeviceDto> devices)
        : IMicrophoneCaptureController
    {
        public IMicrophonePcmSource? CurrentSource => null;

        public Task<IReadOnlyList<AudioInputDeviceDto>> ListInputDevicesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(devices);
        }

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

        public MicrophoneCaptureSnapshot GetSnapshot() => MicrophoneCaptureSnapshot.Stopped();
    }
}

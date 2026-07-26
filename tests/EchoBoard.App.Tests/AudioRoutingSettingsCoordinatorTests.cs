using EchoBoard.App.ViewModels;
using EchoBoard.Application.Audio;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EchoBoard.App.Tests;

public sealed class AudioRoutingSettingsCoordinatorTests
{
    [Fact]
    public async Task SharedSettingsApplyToMixerAndPersistCompleteSnapshot()
    {
        var settings = new FakeSettings();
        var engine = new FakeRoutingEngine();
        var services = new ServiceCollection()
            .AddSingleton<IAppSettingRepository>(settings)
            .AddSingleton<IMicrophoneCaptureController, FakeMicrophone>()
            .AddSingleton<IAudioOutputDeviceEnumerator, FakeOutputs>()
            .AddSingleton<IAudioRoutingEngine>(engine)
            .AddTransient<LoadAudioRoutingSettingsUseCase>()
            .AddTransient<SaveAudioRoutingSettingsUseCase>()
            .BuildServiceProvider();
        await using var provider = services;
        using var coordinator = new AudioRoutingSettingsCoordinator(
            provider.GetRequiredService<IServiceScopeFactory>(),
            engine,
            NullLogger<AudioRoutingSettingsCoordinator>.Instance);
        await coordinator.LoadAsync(CancellationToken.None);

        coordinator.EffectsPercent = 35;
        coordinator.MonitorPercent = 42;
        coordinator.IsMonitorMuted = true;
        await coordinator.FlushAsync();

        coordinator.EffectsPercent.Should().Be(35);
        coordinator.MonitorPercent.Should().Be(42);
        coordinator.IsMonitorMuted.Should().BeTrue();
        engine.Applied.Should().NotBeNull();
        engine.Applied!.EffectsVolume.Should().BeApproximately(0.35, 0.001);
        engine.Applied.IsMonitorMuted.Should().BeTrue();
        settings.Values[AudioRoutingSettingKeys.EffectsVolume].Should().Be("0.35");
        settings.Values[AudioRoutingSettingKeys.IsMonitorMuted].Should().Be("True");
    }

    private sealed class FakeSettings : IAppSettingRepository
    {
        public Dictionary<string, string> Values { get; } = [];

        public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult(Values.GetValueOrDefault(key));

        public Task UpsertValueAsync(string key, string value, CancellationToken cancellationToken)
        {
            Values[key] = value;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOutputs : IAudioOutputDeviceEnumerator
    {
        public Task<IReadOnlyList<AudioOutputDeviceDto>> ListOutputDevicesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AudioOutputDeviceDto>>([]);
    }

    private sealed class FakeRoutingEngine : IAudioRoutingEngine
    {
        public AudioRoutingSettingsDto? Applied { get; private set; }

        public Task InitializeAsync(AudioRoutingSettingsDto settings, CancellationToken cancellationToken)
        {
            Applied = settings;
            return Task.CompletedTask;
        }

        public Task ApplySettingsAsync(AudioRoutingSettingsDto settings, CancellationToken cancellationToken)
        {
            Applied = settings;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public AudioRoutingSnapshot GetRoutingSnapshot() => AudioRoutingSnapshot.Stopped;
    }

    private sealed class FakeMicrophone : IMicrophoneCaptureController
    {
        public IMicrophonePcmSource? CurrentSource => null;
        public Task<IReadOnlyList<AudioInputDeviceDto>> ListInputDevicesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AudioInputDeviceDto>>([]);
        public Task RestoreSelectionAsync(MicrophoneSettingsDto settings, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SelectDeviceAsync(AudioInputDeviceDto device, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetGainAsync(double gain, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetMutedAsync(bool isMuted, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public MicrophoneCaptureSnapshot GetSnapshot() => MicrophoneCaptureSnapshot.Stopped();
    }
}

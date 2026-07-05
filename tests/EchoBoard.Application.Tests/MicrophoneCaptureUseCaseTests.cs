using EchoBoard.Application.Audio;
using FluentAssertions;
using Xunit;

namespace EchoBoard.Application.Tests;

public sealed class MicrophoneCaptureUseCaseTests
{
    [Fact]
    public async Task LoadSettingsRestoresPersistedMicrophoneWhenAvailable()
    {
        var settings = new FakeAppSettingRepository();
        await settings.UpsertValueAsync(MicrophoneSettingKeys.SelectedDeviceId, "mic-1", CancellationToken.None);
        await settings.UpsertValueAsync(MicrophoneSettingKeys.SelectedDeviceName, "Studio Mic", CancellationToken.None);
        await settings.UpsertValueAsync(MicrophoneSettingKeys.Gain, "0.42", CancellationToken.None);
        await settings.UpsertValueAsync(MicrophoneSettingKeys.IsMuted, "true", CancellationToken.None);
        var controller = new FakeMicrophoneCaptureController();
        controller.Devices.Add(new AudioInputDeviceDto("mic-1", "Studio Mic", IsDefault: true, IsAvailable: true));
        var useCase = new LoadMicrophoneSettingsUseCase(settings, controller);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        result.Should().Be(new MicrophoneSettingsDto("mic-1", "Studio Mic", 0.42, IsMuted: true));
        controller.RestoredSettings.Should().Be(result);
        controller.Snapshot.State.Should().Be(MicrophoneCaptureState.Stopped);
        controller.Snapshot.SelectedDeviceName.Should().Be("Studio Mic");
    }

    [Fact]
    public async Task LoadSettingsReportsPersistedMicrophoneAsUnavailableWhenMissing()
    {
        var settings = new FakeAppSettingRepository();
        await settings.UpsertValueAsync(MicrophoneSettingKeys.SelectedDeviceId, "missing-mic", CancellationToken.None);
        await settings.UpsertValueAsync(MicrophoneSettingKeys.SelectedDeviceName, "Old Headset", CancellationToken.None);
        var controller = new FakeMicrophoneCaptureController();
        var useCase = new LoadMicrophoneSettingsUseCase(settings, controller);

        await useCase.ExecuteAsync(CancellationToken.None);

        controller.Snapshot.State.Should().Be(MicrophoneCaptureState.Unavailable);
        controller.Snapshot.StatusMessage.Should().Be("Previous microphone unavailable: Old Headset");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public async Task SetGainRejectsValuesOutsideNormalizedRange(double gain)
    {
        var useCase = new SetMicrophoneGainUseCase(new FakeAppSettingRepository(), new FakeMicrophoneCaptureController());

        var act = () => useCase.ExecuteAsync(gain, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName(nameof(gain));
    }

    [Fact]
    public async Task SetGainPersistsAndAppliesNormalizedGain()
    {
        var settings = new FakeAppSettingRepository();
        var controller = new FakeMicrophoneCaptureController();
        var useCase = new SetMicrophoneGainUseCase(settings, controller);

        await useCase.ExecuteAsync(0.35, CancellationToken.None);

        (await settings.GetValueAsync(MicrophoneSettingKeys.Gain, CancellationToken.None)).Should().Be("0.35");
        controller.Snapshot.Gain.Should().Be(0.35);
    }

    [Fact]
    public async Task StartCaptureWithoutSelectedDeviceReturnsUnavailableSnapshot()
    {
        var controller = new FakeMicrophoneCaptureController();
        var useCase = new StartMicrophoneCaptureUseCase(controller);

        var snapshot = await useCase.ExecuteAsync(CancellationToken.None);

        snapshot.State.Should().Be(MicrophoneCaptureState.Unavailable);
        snapshot.StatusMessage.Should().Be("Select a microphone before starting capture.");
    }

    [Fact]
    public async Task SelectMicrophonePersistsDeviceAndUpdatesController()
    {
        var settings = new FakeAppSettingRepository();
        var controller = new FakeMicrophoneCaptureController();
        controller.Devices.Add(new AudioInputDeviceDto("mic-1", "Desk Mic", IsDefault: false, IsAvailable: true));
        var useCase = new SelectMicrophoneDeviceUseCase(settings, controller);

        await useCase.ExecuteAsync("mic-1", CancellationToken.None);

        (await settings.GetValueAsync(MicrophoneSettingKeys.SelectedDeviceId, CancellationToken.None)).Should().Be("mic-1");
        (await settings.GetValueAsync(MicrophoneSettingKeys.SelectedDeviceName, CancellationToken.None)).Should().Be("Desk Mic");
        controller.Snapshot.SelectedDeviceName.Should().Be("Desk Mic");
    }

    private sealed class FakeAppSettingRepository : IAppSettingRepository
    {
        private readonly Dictionary<string, string> values = new(StringComparer.Ordinal);

        public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
        {
            values.TryGetValue(key, out var value);
            return Task.FromResult<string?>(value);
        }

        public Task UpsertValueAsync(string key, string value, CancellationToken cancellationToken)
        {
            values[key] = value;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMicrophoneCaptureController : IMicrophoneCaptureController
    {
        public List<AudioInputDeviceDto> Devices { get; } = [];

        public MicrophoneSettingsDto? RestoredSettings { get; private set; }

        public MicrophoneCaptureSnapshot Snapshot { get; private set; } = MicrophoneCaptureSnapshot.Stopped();

        public IMicrophonePcmSource? CurrentSource => null;

        public Task<IReadOnlyList<AudioInputDeviceDto>> ListInputDevicesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AudioInputDeviceDto>>(Devices);
        }

        public Task RestoreSelectionAsync(MicrophoneSettingsDto settings, CancellationToken cancellationToken)
        {
            RestoredSettings = settings;
            var device = Devices.SingleOrDefault(item => item.Id == settings.SelectedDeviceId);
            Snapshot = device is null && !string.IsNullOrWhiteSpace(settings.SelectedDeviceName)
                ? MicrophoneCaptureSnapshot.Unavailable($"Previous microphone unavailable: {settings.SelectedDeviceName}", settings)
                : MicrophoneCaptureSnapshot.Stopped(device, settings);
            return Task.CompletedTask;
        }

        public Task SelectDeviceAsync(AudioInputDeviceDto device, CancellationToken cancellationToken)
        {
            Snapshot = MicrophoneCaptureSnapshot.Stopped(device, Snapshot.Settings with
            {
                SelectedDeviceId = device.Id,
                SelectedDeviceName = device.Name
            });
            return Task.CompletedTask;
        }

        public Task SetGainAsync(double gain, CancellationToken cancellationToken)
        {
            Snapshot = Snapshot with { Gain = gain };
            return Task.CompletedTask;
        }

        public Task SetMutedAsync(bool isMuted, CancellationToken cancellationToken)
        {
            Snapshot = Snapshot with { IsMuted = isMuted };
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Snapshot = string.IsNullOrWhiteSpace(Snapshot.SelectedDeviceId)
                ? Snapshot with
                {
                    State = MicrophoneCaptureState.Unavailable,
                    StatusMessage = "Select a microphone before starting capture."
                }
                : Snapshot with { State = MicrophoneCaptureState.Active };
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Snapshot = Snapshot with { State = MicrophoneCaptureState.Stopped, Level = 0 };
            return Task.CompletedTask;
        }

        public MicrophoneCaptureSnapshot GetSnapshot()
        {
            return Snapshot;
        }
    }
}

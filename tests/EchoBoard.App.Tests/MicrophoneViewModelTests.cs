using EchoBoard.App.Controls;
using EchoBoard.App.ViewModels;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Hotkeys;
using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace EchoBoard.App.Tests;

public sealed class MicrophoneViewModelTests
{
    [Fact]
    public async Task SettingsViewModelLoadsMicrophoneDevicesAndSnapshot()
    {
        var settings = new FakeAppSettingRepository();
        var controller = new FakeMicrophoneCaptureController();
        controller.Devices.Add(new AudioInputDeviceDto("mic-1", "Desk Mic", IsDefault: true, IsAvailable: true));
        var viewModel = CreateSettingsViewModel(settings, controller);

        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.MicrophoneDevices.Should().ContainSingle(device => device.Id == "mic-1" && device.Name == "Desk Mic");
        viewModel.MicrophoneStatusText.Should().Be("Stopped");
        viewModel.MicrophoneLevel.Should().Be(0);
        viewModel.StartMicrophoneCaptureCommand.Should().NotBeNull();
        viewModel.StopMicrophoneCaptureCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task SettingsViewModelStartsAndStopsMicrophoneCapture()
    {
        var controller = new FakeMicrophoneCaptureController();
        controller.Devices.Add(new AudioInputDeviceDto("mic-1", "Desk Mic", IsDefault: true, IsAvailable: true));
        controller.Snapshot = MicrophoneCaptureSnapshot.Stopped(
            controller.Devices.Single(),
            new MicrophoneSettingsDto("mic-1", "Desk Mic", 1.0, false));
        var viewModel = CreateSettingsViewModel(new FakeAppSettingRepository(), controller);
        await viewModel.LoadAsync(CancellationToken.None);

        await viewModel.StartMicrophoneCaptureAsync(CancellationToken.None);

        viewModel.MicrophoneStatusText.Should().Be("Active");

        await viewModel.StopMicrophoneCaptureAsync(CancellationToken.None);

        viewModel.MicrophoneStatusText.Should().Be("Stopped");
        viewModel.MicrophoneLevel.Should().Be(0);
    }

    [Fact]
    public void SettingsViewModelUsesFastAttackAndGradualReleaseForMeterLevel()
    {
        var controller = new FakeMicrophoneCaptureController
        {
            Snapshot = new MicrophoneCaptureSnapshot(
                MicrophoneCaptureState.Active,
                "mic-1",
                "Desk Mic",
                1.0,
                1.0,
                IsMuted: false,
                "Capturing",
                null,
                new AudioStreamFormatDto(48000, 1, 32, "IeeeFloat"))
        };
        controller.Devices.Add(new AudioInputDeviceDto("mic-1", "Desk Mic", IsDefault: true, IsAvailable: true));
        var viewModel = CreateSettingsViewModel(new FakeAppSettingRepository(), controller);

        viewModel.RefreshMicrophoneSnapshot(TimeSpan.FromMilliseconds(33));
        var attackLevel = viewModel.MicrophoneLevel;

        attackLevel.Should().BeGreaterThan(0.8);
        viewModel.RefreshMicrophoneSnapshot(TimeSpan.FromMilliseconds(33));
        viewModel.RefreshMicrophoneSnapshot(TimeSpan.FromMilliseconds(33));
        viewModel.MicrophoneLevel.Should().BeGreaterThan(0.99, "the meter must respond within 100 ms");

        attackLevel = viewModel.MicrophoneLevel;
        controller.Snapshot = controller.Snapshot with { Level = 0 };
        viewModel.RefreshMicrophoneSnapshot(TimeSpan.FromMilliseconds(33));

        viewModel.MicrophoneLevel.Should().BeGreaterThan(0);
        viewModel.MicrophoneLevel.Should().BeLessThan(attackLevel);
    }

    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(0.5, 0.5)]
    [InlineData(1.5, 1.0)]
    [InlineData(double.NaN, 0.0)]
    public void MicrophoneLevelSmootherNormalizesTargets(double target, double expected)
    {
        var smoother = new MicrophoneLevelSmoother();

        smoother.Reset(target);

        smoother.Value.Should().Be(expected);
    }

    [Fact]
    public void MicrophoneLevelSmootherSettlesAtZeroAfterRelease()
    {
        var smoother = new MicrophoneLevelSmoother();
        smoother.Reset(1.0);

        for (var frame = 0; frame < 30; frame++)
        {
            smoother.Update(0, TimeSpan.FromMilliseconds(33));
        }

        smoother.Value.Should().Be(0);
    }

    [Fact]
    public void AudioDiagnosticsViewModelMapsMicrophoneSnapshotToDisplayState()
    {
        var controller = new FakeMicrophoneCaptureController
        {
            Snapshot = new MicrophoneCaptureSnapshot(
                MicrophoneCaptureState.Active,
                "mic-1",
                "Desk Mic",
                0.7,
                0.8,
                IsMuted: false,
                "Capturing",
                null,
                new AudioStreamFormatDto(48000, 1, 32, "IeeeFloat"))
        };
        var viewModel = new AudioDiagnosticsViewModel(new GetMicrophoneCaptureSnapshotUseCase(controller));

        viewModel.Refresh();

        viewModel.MicrophoneDevice.DeviceName.Should().Be("Desk Mic");
        viewModel.MicrophoneDevice.Status.Should().Be(DeviceStatusKind.Connected);
        viewModel.MicrophoneMeter.Level.Should().Be(0.7);
        viewModel.FormatText.Should().Be("48000 Hz, 1 ch, 32-bit IeeeFloat");
    }

    private static SettingsViewModel CreateSettingsViewModel(
        IAppSettingRepository settings,
        IMicrophoneCaptureController controller)
    {
        var hotkeys = new FakeHotkeyBindingRepository();
        var runtime = new FakeHotkeyRuntime();

        return new SettingsViewModel(
            new ListHotkeyBindingsUseCase(hotkeys, runtime),
            new AssignGlobalHotkeyUseCase(hotkeys, runtime),
            new RemoveHotkeyBindingUseCase(hotkeys, runtime),
            new SetHotkeyBindingEnabledUseCase(hotkeys, runtime),
            new ListMicrophoneDevicesUseCase(controller),
            new LoadMicrophoneSettingsUseCase(settings, controller),
            new SelectMicrophoneDeviceUseCase(settings, controller),
            new SetMicrophoneGainUseCase(settings, controller),
            new SetMicrophoneMuteUseCase(settings, controller),
            new StartMicrophoneCaptureUseCase(controller),
            new StopMicrophoneCaptureUseCase(controller),
            new GetMicrophoneCaptureSnapshotUseCase(controller));
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

        public MicrophoneCaptureSnapshot Snapshot { get; set; } = MicrophoneCaptureSnapshot.Stopped();

        public IMicrophonePcmSource? CurrentSource => null;

        public Task<IReadOnlyList<AudioInputDeviceDto>> ListInputDevicesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AudioInputDeviceDto>>(Devices);
        }

        public Task RestoreSelectionAsync(MicrophoneSettingsDto settings, CancellationToken cancellationToken)
        {
            var device = Devices.SingleOrDefault(item => item.Id == settings.SelectedDeviceId) ?? Devices.FirstOrDefault();
            Snapshot = MicrophoneCaptureSnapshot.Stopped(device, settings);
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
            Snapshot = Snapshot with { State = MicrophoneCaptureState.Active, StatusMessage = "Capturing" };
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Snapshot = Snapshot with { State = MicrophoneCaptureState.Stopped, Level = 0, StatusMessage = "Stopped" };
            return Task.CompletedTask;
        }

        public MicrophoneCaptureSnapshot GetSnapshot()
        {
            return Snapshot;
        }
    }

    private sealed class FakeHotkeyBindingRepository : IHotkeyBindingRepository
    {
        public Task<IReadOnlyList<HotkeyBinding>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<HotkeyBinding>>([]);

        public Task<HotkeyBinding?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<HotkeyBinding?>(null);

        public Task<HotkeyBinding?> GetForSoundAsync(Guid soundId, CancellationToken cancellationToken) => Task.FromResult<HotkeyBinding?>(null);

        public Task<HotkeyBinding?> GetForGlobalCommandAsync(GlobalHotkeyCommand command, CancellationToken cancellationToken) => Task.FromResult<HotkeyBinding?>(null);

        public Task<bool> CombinationExistsAsync(string normalizedKeyCombination, Guid? excludingBindingId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task AddAsync(HotkeyBinding binding, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateAsync(HotkeyBinding binding, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeHotkeyRuntime : IHotkeyRuntimeService
    {
        public Task<HotkeyRegistrationResult> RegisterBindingAsync(HotkeyBinding binding, CancellationToken cancellationToken)
        {
            return Task.FromResult(HotkeyRegistrationResult.Active("Registered."));
        }

        public Task UnregisterBindingAsync(Guid bindingId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public HotkeyRegistrationState GetRegistrationState(Guid bindingId)
        {
            return HotkeyRegistrationState.Active;
        }
    }
}

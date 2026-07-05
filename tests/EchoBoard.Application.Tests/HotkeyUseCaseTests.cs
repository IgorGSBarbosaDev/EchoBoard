using EchoBoard.Application.Hotkeys;
using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Enums;
using EchoBoard.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace EchoBoard.Application.Tests;

public sealed class HotkeyUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AssignSoundHotkeyRejectsDuplicateCombination()
    {
        var sounds = new FakeSoundLibraryRepository();
        var hotkeys = new FakeHotkeyBindingRepository();
        var runtime = new FakeHotkeyRuntime();
        var firstSound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, null, 0, Now);
        var secondSound = Sound.Create("Alert", "C:\\Audio\\alert.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, null, 1, Now);
        await sounds.AddSoundAsync(firstSound, CancellationToken.None);
        await sounds.AddSoundAsync(secondSound, CancellationToken.None);
        await hotkeys.AddAsync(HotkeyBinding.CreateForSound(
            firstSound.Id,
            HotkeyCombination.Create(HotkeyModifiers.Control, "S"),
            isEnabled: true,
            Now), CancellationToken.None);
        var useCase = new AssignSoundHotkeyUseCase(hotkeys, sounds, runtime);

        var act = () => useCase.ExecuteAsync(
            new AssignSoundHotkeyRequest(secondSound.Id, HotkeyModifiers.Control, "S", IsEnabled: true, Now),
            CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateHotkeyBindingException>();
        runtime.RegisteredBindings.Should().BeEmpty();
    }

    [Fact]
    public async Task AssignSoundHotkeyPersistsAndRegistersEnabledBinding()
    {
        var sounds = new FakeSoundLibraryRepository();
        var hotkeys = new FakeHotkeyBindingRepository();
        var runtime = new FakeHotkeyRuntime();
        var sound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, null, 0, Now);
        await sounds.AddSoundAsync(sound, CancellationToken.None);
        var useCase = new AssignSoundHotkeyUseCase(hotkeys, sounds, runtime);

        var result = await useCase.ExecuteAsync(
            new AssignSoundHotkeyRequest(sound.Id, HotkeyModifiers.Control | HotkeyModifiers.Alt, "F8", IsEnabled: true, Now),
            CancellationToken.None);

        result.NormalizedKeyCombination.Should().Be("Ctrl+Alt+F8");
        result.RegistrationState.Should().Be(HotkeyRegistrationState.Active);
        hotkeys.Items.Should().ContainSingle(binding => binding.SoundId == sound.Id);
        runtime.RegisteredBindings.Should().ContainSingle(binding => binding.Id == result.Id);
    }

    [Fact]
    public async Task DisabledBindingIsPersistedAndUnregistered()
    {
        var sounds = new FakeSoundLibraryRepository();
        var hotkeys = new FakeHotkeyBindingRepository();
        var runtime = new FakeHotkeyRuntime();
        var sound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, null, 0, Now);
        await sounds.AddSoundAsync(sound, CancellationToken.None);
        var useCase = new AssignSoundHotkeyUseCase(hotkeys, sounds, runtime);

        var result = await useCase.ExecuteAsync(
            new AssignSoundHotkeyRequest(sound.Id, HotkeyModifiers.Shift, "F9", IsEnabled: false, Now),
            CancellationToken.None);

        result.RegistrationState.Should().Be(HotkeyRegistrationState.Disabled);
        hotkeys.Items.Should().ContainSingle(binding => !binding.IsEnabled);
        runtime.RegisteredBindings.Should().BeEmpty();
        runtime.UnregisteredBindingIds.Should().Contain(result.Id);
    }

    [Fact]
    public async Task RemoveBindingUnregistersAndDeletesBinding()
    {
        var hotkeys = new FakeHotkeyBindingRepository();
        var runtime = new FakeHotkeyRuntime();
        var binding = HotkeyBinding.CreateForGlobalCommand(
            GlobalHotkeyCommand.ShowHideMainWindow,
            HotkeyCombination.Create(HotkeyModifiers.Alt, "F10"),
            isEnabled: true,
            Now);
        await hotkeys.AddAsync(binding, CancellationToken.None);
        var useCase = new RemoveHotkeyBindingUseCase(hotkeys, runtime);

        await useCase.ExecuteAsync(binding.Id, CancellationToken.None);

        hotkeys.Items.Should().BeEmpty();
        runtime.UnregisteredBindingIds.Should().Contain(binding.Id);
    }

    [Fact]
    public async Task RuntimeRestoresEnabledBindingsAndCapturesRegistrationFailure()
    {
        var registrar = new FakeGlobalHotkeyRegistrar();
        registrar.NextState = HotkeyRegistrationState.Conflicting;
        var runtime = new HotkeyRuntimeService(
            registrar,
            new FakeSoundPlaybackPort(),
            new FakePlaybackControlPort(),
            new FakeShellWindowPort());
        var binding = HotkeyBinding.CreateForGlobalCommand(
            GlobalHotkeyCommand.StopAllSounds,
            HotkeyCombination.Create(HotkeyModifiers.Control, "F12"),
            isEnabled: true,
            Now);

        var result = await runtime.RegisterBindingAsync(binding, CancellationToken.None);

        result.State.Should().Be(HotkeyRegistrationState.Conflicting);
        runtime.GetRegistrationState(binding.Id).Should().Be(HotkeyRegistrationState.Conflicting);
    }

    [Fact]
    public async Task RuntimeDispatchesGlobalCommandsToPorts()
    {
        var registrar = new FakeGlobalHotkeyRegistrar();
        var shell = new FakeShellWindowPort();
        var runtime = new HotkeyRuntimeService(
            registrar,
            new FakeSoundPlaybackPort(),
            new FakePlaybackControlPort(),
            shell);
        var binding = HotkeyBinding.CreateForGlobalCommand(
            GlobalHotkeyCommand.ShowHideMainWindow,
            HotkeyCombination.Create(HotkeyModifiers.Control, "F11"),
            isEnabled: true,
            Now);
        await runtime.RegisterBindingAsync(binding, CancellationToken.None);

        registrar.RaisePressed(binding.Id);

        shell.ToggleCount.Should().Be(1);
    }

    private sealed class FakeHotkeyBindingRepository : IHotkeyBindingRepository
    {
        private readonly List<HotkeyBinding> bindings = [];

        public IReadOnlyList<HotkeyBinding> Items => bindings;

        public Task<IReadOnlyList<HotkeyBinding>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<HotkeyBinding>>(bindings.ToArray());
        }

        public Task<HotkeyBinding?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(bindings.SingleOrDefault(binding => binding.Id == id));
        }

        public Task<HotkeyBinding?> GetForSoundAsync(Guid soundId, CancellationToken cancellationToken)
        {
            return Task.FromResult(bindings.SingleOrDefault(binding => binding.SoundId == soundId));
        }

        public Task<HotkeyBinding?> GetForGlobalCommandAsync(GlobalHotkeyCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(bindings.SingleOrDefault(binding => binding.GlobalCommand == command));
        }

        public Task<bool> CombinationExistsAsync(string normalizedKeyCombination, Guid? excludingBindingId, CancellationToken cancellationToken)
        {
            return Task.FromResult(bindings.Any(binding =>
                binding.Id != excludingBindingId &&
                string.Equals(binding.NormalizedKeyCombination, normalizedKeyCombination, StringComparison.OrdinalIgnoreCase)));
        }

        public Task AddAsync(HotkeyBinding binding, CancellationToken cancellationToken)
        {
            bindings.Add(binding);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(HotkeyBinding binding, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            bindings.RemoveAll(binding => binding.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSoundLibraryRepository : ISoundLibraryRepository
    {
        private readonly List<Sound> sounds = [];

        public Task<IReadOnlyList<Sound>> ListSoundsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Sound>>(sounds);

        public Task<Sound?> GetSoundAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(sounds.SingleOrDefault(sound => sound.Id == id));

        public Task<bool> SoundFilePathExistsAsync(string filePath, Guid? excludingSoundId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task AddSoundAsync(Sound sound, CancellationToken cancellationToken)
        {
            sounds.Add(sound);
            return Task.CompletedTask;
        }

        public Task UpdateSoundAsync(Sound sound, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteSoundAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeHotkeyRuntime : IHotkeyRuntimeService
    {
        public List<HotkeyBinding> RegisteredBindings { get; } = [];

        public List<Guid> UnregisteredBindingIds { get; } = [];

        public Task<HotkeyRegistrationResult> RegisterBindingAsync(HotkeyBinding binding, CancellationToken cancellationToken)
        {
            if (binding.IsEnabled)
            {
                RegisteredBindings.Add(binding);
                return Task.FromResult(new HotkeyRegistrationResult(HotkeyRegistrationState.Active, "Registered."));
            }

            UnregisteredBindingIds.Add(binding.Id);
            return Task.FromResult(new HotkeyRegistrationResult(HotkeyRegistrationState.Disabled, "Disabled."));
        }

        public Task UnregisterBindingAsync(Guid bindingId, CancellationToken cancellationToken)
        {
            UnregisteredBindingIds.Add(bindingId);
            return Task.CompletedTask;
        }

        public HotkeyRegistrationState GetRegistrationState(Guid bindingId) => HotkeyRegistrationState.Active;
    }

    private sealed class FakeGlobalHotkeyRegistrar : IGlobalHotkeyRegistrar
    {
        public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

        public HotkeyRegistrationState NextState { get; set; } = HotkeyRegistrationState.Active;

        public Task<HotkeyRegistrationResult> RegisterAsync(HotkeyRegistrationRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HotkeyRegistrationResult(NextState, NextState == HotkeyRegistrationState.Active ? "Registered." : "Failed."));
        }

        public Task UnregisterAsync(Guid bindingId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UnregisterAllAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void RaisePressed(Guid bindingId)
        {
            HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(bindingId));
        }
    }

    private sealed class FakeSoundPlaybackPort : ISoundPlaybackCommandPort
    {
        public Task<HotkeyCommandResult> PlaySoundAsync(Guid soundId, CancellationToken cancellationToken)
        {
            return Task.FromResult(HotkeyCommandResult.Unavailable("Playback unavailable."));
        }
    }

    private sealed class FakePlaybackControlPort : IPlaybackControlCommandPort
    {
        public Task<HotkeyCommandResult> StopAllSoundsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(HotkeyCommandResult.Unavailable("Playback unavailable."));
        }

        public Task<HotkeyCommandResult> PauseResumePlaybackAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(HotkeyCommandResult.Unavailable("Playback unavailable."));
        }
    }

    private sealed class FakeShellWindowPort : IShellWindowCommandPort
    {
        public int ToggleCount { get; private set; }

        public Task<HotkeyCommandResult> ShowOrHideMainWindowAsync(CancellationToken cancellationToken)
        {
            ToggleCount++;
            return Task.FromResult(HotkeyCommandResult.Success("Window toggled."));
        }
    }
}

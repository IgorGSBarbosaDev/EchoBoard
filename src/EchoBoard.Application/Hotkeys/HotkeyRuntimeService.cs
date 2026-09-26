using System.Collections.Concurrent;
using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Enums;

namespace EchoBoard.Application.Hotkeys;

public sealed class HotkeyRuntimeService : IHotkeyRuntimeService, IAsyncDisposable
{
    private readonly IGlobalHotkeyRegistrar registrar;
    private readonly ISoundPlaybackCommandPort soundPlayback;
    private readonly ConcurrentDictionary<Guid, HotkeyBinding> activeBindings = new();
    private readonly ConcurrentDictionary<Guid, HotkeyRegistrationState> registrationStates = new();

    public HotkeyRuntimeService(
        IGlobalHotkeyRegistrar registrar,
        ISoundPlaybackCommandPort soundPlayback)
    {
        this.registrar = registrar;
        this.soundPlayback = soundPlayback;
        this.registrar.HotkeyPressed += OnHotkeyPressed;
    }

    public async Task<HotkeyRegistrationResult> RegisterBindingAsync(HotkeyBinding binding, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binding);

        await UnregisterBindingAsync(binding.Id, cancellationToken);
        if (!binding.IsEnabled)
        {
            registrationStates[binding.Id] = HotkeyRegistrationState.Disabled;
            return HotkeyRegistrationResult.Disabled("Hotkey is disabled.");
        }

        var result = await registrar.RegisterAsync(
            new HotkeyRegistrationRequest(binding.Id, binding.Modifiers, binding.PrimaryKey, binding.NormalizedKeyCombination),
            cancellationToken);

        registrationStates[binding.Id] = result.State;
        if (result.State == HotkeyRegistrationState.Active)
        {
            activeBindings[binding.Id] = binding;
        }

        return result;
    }

    public async Task UnregisterBindingAsync(Guid bindingId, CancellationToken cancellationToken)
    {
        activeBindings.TryRemove(bindingId, out _);
        registrationStates.TryRemove(bindingId, out _);
        await registrar.UnregisterAsync(bindingId, cancellationToken);
    }

    public HotkeyRegistrationState GetRegistrationState(Guid bindingId)
    {
        return registrationStates.TryGetValue(bindingId, out var state) ? state : HotkeyRegistrationState.Unavailable;
    }

    public async ValueTask DisposeAsync()
    {
        registrar.HotkeyPressed -= OnHotkeyPressed;
        await registrar.UnregisterAllAsync(CancellationToken.None);
    }

    private void OnHotkeyPressed(object? sender, HotkeyPressedEventArgs e)
    {
        if (!activeBindings.TryGetValue(e.BindingId, out var binding))
        {
            return;
        }

        _ = DispatchAsync(binding, CancellationToken.None);
    }

    private Task<HotkeyCommandResult> DispatchAsync(HotkeyBinding binding, CancellationToken cancellationToken)
    {
        return binding.TargetKind == HotkeyBindingTargetKind.Sound && binding.SoundId is not null
            ? soundPlayback.PlaySoundAsync(binding.SoundId.Value, cancellationToken)
            : Task.FromResult(HotkeyCommandResult.Failed("Hotkey target is invalid."));
    }
}

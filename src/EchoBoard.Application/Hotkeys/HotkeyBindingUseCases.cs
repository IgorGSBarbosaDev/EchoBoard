using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Enums;
using EchoBoard.Domain.ValueObjects;

namespace EchoBoard.Application.Hotkeys;

public sealed class ListHotkeyBindingsUseCase
{
    private readonly IHotkeyBindingRepository hotkeys;
    private readonly IHotkeyRuntimeService runtime;

    public ListHotkeyBindingsUseCase(IHotkeyBindingRepository hotkeys, IHotkeyRuntimeService runtime)
    {
        this.hotkeys = hotkeys;
        this.runtime = runtime;
    }

    public async Task<IReadOnlyList<HotkeyBindingDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var bindings = await hotkeys.ListAsync(cancellationToken);
        return bindings.Select(binding => Map(binding, runtime.GetRegistrationState(binding.Id), RegistrationMessage(binding))).ToArray();
    }

    private static string RegistrationMessage(HotkeyBinding binding)
    {
        return binding.IsEnabled ? "Registered." : "Disabled.";
    }

    internal static HotkeyBindingDto Map(HotkeyBinding binding, HotkeyRegistrationState state, string message)
    {
        return new HotkeyBindingDto(
            binding.Id,
            binding.TargetKind,
            binding.SoundId,
            binding.GlobalCommand,
            binding.NormalizedKeyCombination,
            binding.Modifiers,
            binding.PrimaryKey,
            binding.IsEnabled,
            binding.IsEnabled ? state : HotkeyRegistrationState.Disabled,
            binding.IsEnabled ? message : "Disabled.",
            binding.CreatedAt,
            binding.UpdatedAt);
    }
}

public sealed class AssignSoundHotkeyUseCase
{
    private readonly IHotkeyBindingRepository hotkeys;
    private readonly ISoundLibraryRepository sounds;
    private readonly IHotkeyRuntimeService runtime;

    public AssignSoundHotkeyUseCase(IHotkeyBindingRepository hotkeys, ISoundLibraryRepository sounds, IHotkeyRuntimeService runtime)
    {
        this.hotkeys = hotkeys;
        this.sounds = sounds;
        this.runtime = runtime;
    }

    public async Task<HotkeyBindingDto> ExecuteAsync(AssignSoundHotkeyRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sound = await sounds.GetSoundAsync(request.SoundId, cancellationToken);
        if (sound is null)
        {
            throw new SoundNotFoundException(request.SoundId);
        }

        var combination = HotkeyCombination.Create(request.Modifiers, request.PrimaryKey);
        var existing = await hotkeys.GetForSoundAsync(request.SoundId, cancellationToken);
        await EnsureCombinationAvailableAsync(combination.NormalizedText, existing?.Id, cancellationToken);

        var binding = existing ?? HotkeyBinding.CreateForSound(request.SoundId, combination, request.IsEnabled, request.UpdatedAt);
        if (existing is not null)
        {
            await runtime.UnregisterBindingAsync(existing.Id, cancellationToken);
            binding.ChangeCombination(combination, request.UpdatedAt);
            binding.SetEnabled(request.IsEnabled, request.UpdatedAt);
            await hotkeys.UpdateAsync(binding, cancellationToken);
        }
        else
        {
            await hotkeys.AddAsync(binding, cancellationToken);
        }

        var registration = await runtime.RegisterBindingAsync(binding, cancellationToken);
        return ListHotkeyBindingsUseCase.Map(binding, registration.State, registration.Message);
    }

    private async Task EnsureCombinationAvailableAsync(string normalizedKeyCombination, Guid? excludingBindingId, CancellationToken cancellationToken)
    {
        if (await hotkeys.CombinationExistsAsync(normalizedKeyCombination, excludingBindingId, cancellationToken))
        {
            throw new DuplicateHotkeyBindingException(normalizedKeyCombination);
        }
    }
}

public sealed class AssignGlobalHotkeyUseCase
{
    private readonly IHotkeyBindingRepository hotkeys;
    private readonly IHotkeyRuntimeService runtime;

    public AssignGlobalHotkeyUseCase(IHotkeyBindingRepository hotkeys, IHotkeyRuntimeService runtime)
    {
        this.hotkeys = hotkeys;
        this.runtime = runtime;
    }

    public async Task<HotkeyBindingDto> ExecuteAsync(AssignGlobalHotkeyRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var combination = HotkeyCombination.Create(request.Modifiers, request.PrimaryKey);
        var existing = await hotkeys.GetForGlobalCommandAsync(request.Command, cancellationToken);
        if (await hotkeys.CombinationExistsAsync(combination.NormalizedText, existing?.Id, cancellationToken))
        {
            throw new DuplicateHotkeyBindingException(combination.NormalizedText);
        }

        var binding = existing ?? HotkeyBinding.CreateForGlobalCommand(request.Command, combination, request.IsEnabled, request.UpdatedAt);
        if (existing is not null)
        {
            await runtime.UnregisterBindingAsync(existing.Id, cancellationToken);
            binding.ChangeCombination(combination, request.UpdatedAt);
            binding.SetEnabled(request.IsEnabled, request.UpdatedAt);
            await hotkeys.UpdateAsync(binding, cancellationToken);
        }
        else
        {
            await hotkeys.AddAsync(binding, cancellationToken);
        }

        var registration = await runtime.RegisterBindingAsync(binding, cancellationToken);
        return ListHotkeyBindingsUseCase.Map(binding, registration.State, registration.Message);
    }
}

public sealed class SetHotkeyBindingEnabledUseCase
{
    private readonly IHotkeyBindingRepository hotkeys;
    private readonly IHotkeyRuntimeService runtime;

    public SetHotkeyBindingEnabledUseCase(IHotkeyBindingRepository hotkeys, IHotkeyRuntimeService runtime)
    {
        this.hotkeys = hotkeys;
        this.runtime = runtime;
    }

    public async Task<HotkeyBindingDto> ExecuteAsync(Guid id, bool isEnabled, DateTimeOffset updatedAt, CancellationToken cancellationToken)
    {
        var binding = await hotkeys.GetAsync(id, cancellationToken) ?? throw new HotkeyBindingNotFoundException(id);
        await runtime.UnregisterBindingAsync(binding.Id, cancellationToken);
        binding.SetEnabled(isEnabled, updatedAt);
        await hotkeys.UpdateAsync(binding, cancellationToken);
        var registration = await runtime.RegisterBindingAsync(binding, cancellationToken);
        return ListHotkeyBindingsUseCase.Map(binding, registration.State, registration.Message);
    }
}

public sealed class RemoveHotkeyBindingUseCase
{
    private readonly IHotkeyBindingRepository hotkeys;
    private readonly IHotkeyRuntimeService runtime;

    public RemoveHotkeyBindingUseCase(IHotkeyBindingRepository hotkeys, IHotkeyRuntimeService runtime)
    {
        this.hotkeys = hotkeys;
        this.runtime = runtime;
    }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        await runtime.UnregisterBindingAsync(id, cancellationToken);
        await hotkeys.DeleteAsync(id, cancellationToken);
    }
}

public sealed class RestoreHotkeyBindingsUseCase
{
    private readonly IHotkeyBindingRepository hotkeys;
    private readonly IHotkeyRuntimeService runtime;

    public RestoreHotkeyBindingsUseCase(IHotkeyBindingRepository hotkeys, IHotkeyRuntimeService runtime)
    {
        this.hotkeys = hotkeys;
        this.runtime = runtime;
    }

    public async Task<IReadOnlyList<HotkeyBindingDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var bindings = await hotkeys.ListAsync(cancellationToken);
        var results = new List<HotkeyBindingDto>(bindings.Count);
        foreach (var binding in bindings)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var registration = await runtime.RegisterBindingAsync(binding, cancellationToken);
            results.Add(ListHotkeyBindingsUseCase.Map(binding, registration.State, registration.Message));
        }

        return results;
    }
}

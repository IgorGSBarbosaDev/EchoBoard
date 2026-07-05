using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Enums;

namespace EchoBoard.Application.Hotkeys;

public sealed record AssignSoundHotkeyRequest(
    Guid SoundId,
    HotkeyModifiers Modifiers,
    string PrimaryKey,
    bool IsEnabled,
    DateTimeOffset UpdatedAt);

public sealed record AssignGlobalHotkeyRequest(
    GlobalHotkeyCommand Command,
    HotkeyModifiers Modifiers,
    string PrimaryKey,
    bool IsEnabled,
    DateTimeOffset UpdatedAt);

public sealed record HotkeyBindingDto(
    Guid Id,
    HotkeyBindingTargetKind TargetKind,
    Guid? SoundId,
    GlobalHotkeyCommand? GlobalCommand,
    string NormalizedKeyCombination,
    HotkeyModifiers Modifiers,
    string PrimaryKey,
    bool IsEnabled,
    HotkeyRegistrationState RegistrationState,
    string RegistrationMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record HotkeyRegistrationRequest(Guid BindingId, HotkeyModifiers Modifiers, string PrimaryKey, string NormalizedKeyCombination);

public sealed record HotkeyRegistrationResult(HotkeyRegistrationState State, string Message)
{
    public static HotkeyRegistrationResult Active(string message) => new(HotkeyRegistrationState.Active, message);

    public static HotkeyRegistrationResult Disabled(string message) => new(HotkeyRegistrationState.Disabled, message);
}

public sealed class HotkeyPressedEventArgs : EventArgs
{
    public HotkeyPressedEventArgs(Guid bindingId)
    {
        BindingId = bindingId;
    }

    public Guid BindingId { get; }
}

public sealed record HotkeyCommandResult(bool Succeeded, bool IsUnavailable, string Message)
{
    public static HotkeyCommandResult Success(string message) => new(Succeeded: true, IsUnavailable: false, message);

    public static HotkeyCommandResult Unavailable(string message) => new(Succeeded: false, IsUnavailable: true, message);

    public static HotkeyCommandResult Failed(string message) => new(Succeeded: false, IsUnavailable: false, message);
}

public interface IHotkeyBindingRepository
{
    Task<IReadOnlyList<HotkeyBinding>> ListAsync(CancellationToken cancellationToken);

    Task<HotkeyBinding?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<HotkeyBinding?> GetForSoundAsync(Guid soundId, CancellationToken cancellationToken);

    Task<HotkeyBinding?> GetForGlobalCommandAsync(GlobalHotkeyCommand command, CancellationToken cancellationToken);

    Task<bool> CombinationExistsAsync(string normalizedKeyCombination, Guid? excludingBindingId, CancellationToken cancellationToken);

    Task AddAsync(HotkeyBinding binding, CancellationToken cancellationToken);

    Task UpdateAsync(HotkeyBinding binding, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public interface IHotkeyRuntimeService
{
    Task<HotkeyRegistrationResult> RegisterBindingAsync(HotkeyBinding binding, CancellationToken cancellationToken);

    Task UnregisterBindingAsync(Guid bindingId, CancellationToken cancellationToken);

    HotkeyRegistrationState GetRegistrationState(Guid bindingId);
}

public interface IGlobalHotkeyRegistrar
{
    event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    Task<HotkeyRegistrationResult> RegisterAsync(HotkeyRegistrationRequest request, CancellationToken cancellationToken);

    Task UnregisterAsync(Guid bindingId, CancellationToken cancellationToken);

    Task UnregisterAllAsync(CancellationToken cancellationToken);
}

public interface ISoundPlaybackCommandPort
{
    Task<HotkeyCommandResult> PlaySoundAsync(Guid soundId, CancellationToken cancellationToken);
}

public interface IPlaybackControlCommandPort
{
    Task<HotkeyCommandResult> StopAllSoundsAsync(CancellationToken cancellationToken);

    Task<HotkeyCommandResult> PauseResumePlaybackAsync(CancellationToken cancellationToken);
}

public interface IShellWindowCommandPort
{
    Task<HotkeyCommandResult> ShowOrHideMainWindowAsync(CancellationToken cancellationToken);
}

public sealed class DuplicateHotkeyBindingException : InvalidOperationException
{
    public DuplicateHotkeyBindingException(string normalizedKeyCombination)
        : base($"The hotkey {normalizedKeyCombination} is already assigned in EchoBoard.")
    {
        NormalizedKeyCombination = normalizedKeyCombination;
    }

    public string NormalizedKeyCombination { get; }
}

public sealed class HotkeyBindingNotFoundException : InvalidOperationException
{
    public HotkeyBindingNotFoundException(Guid id)
        : base($"Hotkey binding '{id}' was not found.")
    {
    }
}

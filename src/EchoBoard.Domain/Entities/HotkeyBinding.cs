using EchoBoard.Domain.Enums;
using EchoBoard.Domain.Exceptions;
using EchoBoard.Domain.ValueObjects;

namespace EchoBoard.Domain.Entities;

public sealed class HotkeyBinding
{
    private HotkeyBinding()
    {
        NormalizedKeyCombination = string.Empty;
        PrimaryKey = string.Empty;
    }

    private HotkeyBinding(
        Guid id,
        HotkeyBindingTargetKind targetKind,
        Guid? soundId,
        GlobalHotkeyCommand? globalCommand,
        HotkeyCombination combination,
        bool isEnabled,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        TargetKind = targetKind;
        SoundId = soundId;
        GlobalCommand = globalCommand;
        ApplyCombination(combination);
        IsEnabled = isEnabled;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public HotkeyBindingTargetKind TargetKind { get; private set; }

    public Guid? SoundId { get; private set; }

    public GlobalHotkeyCommand? GlobalCommand { get; private set; }

    public string NormalizedKeyCombination { get; private set; } = string.Empty;

    public HotkeyModifiers Modifiers { get; private set; }

    public string PrimaryKey { get; private set; } = string.Empty;

    public bool IsEnabled { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static HotkeyBinding CreateForSound(Guid soundId, HotkeyCombination combination, bool isEnabled, DateTimeOffset createdAt)
    {
        if (soundId == Guid.Empty)
        {
            throw new DomainValidationException("SoundId is required.");
        }

        var utcCreatedAt = ValidateUtc(createdAt, nameof(createdAt));
        return new HotkeyBinding(
            Guid.NewGuid(),
            HotkeyBindingTargetKind.Sound,
            soundId,
            null,
            combination,
            isEnabled,
            utcCreatedAt,
            utcCreatedAt);
    }

    public static HotkeyBinding CreateForGlobalCommand(GlobalHotkeyCommand command, HotkeyCombination combination, bool isEnabled, DateTimeOffset createdAt)
    {
        var utcCreatedAt = ValidateUtc(createdAt, nameof(createdAt));
        return new HotkeyBinding(
            Guid.NewGuid(),
            HotkeyBindingTargetKind.GlobalCommand,
            null,
            command,
            combination,
            isEnabled,
            utcCreatedAt,
            utcCreatedAt);
    }

    public HotkeyCombination ToCombination()
    {
        return HotkeyCombination.FromPersisted(Modifiers, PrimaryKey, NormalizedKeyCombination);
    }

    public void ChangeCombination(HotkeyCombination combination, DateTimeOffset updatedAt)
    {
        ApplyCombination(combination);
        UpdatedAt = ValidateUtc(updatedAt, nameof(updatedAt));
    }

    public void SetEnabled(bool isEnabled, DateTimeOffset updatedAt)
    {
        IsEnabled = isEnabled;
        UpdatedAt = ValidateUtc(updatedAt, nameof(updatedAt));
    }

    private void ApplyCombination(HotkeyCombination combination)
    {
        ArgumentNullException.ThrowIfNull(combination);

        NormalizedKeyCombination = combination.NormalizedText;
        Modifiers = combination.Modifiers;
        PrimaryKey = combination.PrimaryKey;
    }

    private static DateTimeOffset ValidateUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainValidationException($"{parameterName} must be a UTC timestamp.");
        }

        return value;
    }
}

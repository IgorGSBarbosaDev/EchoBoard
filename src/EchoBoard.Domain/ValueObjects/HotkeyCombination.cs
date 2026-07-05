using EchoBoard.Domain.Enums;
using EchoBoard.Domain.Exceptions;

namespace EchoBoard.Domain.ValueObjects;

public sealed record HotkeyCombination
{
    public const int NormalizedTextMaxLength = 64;
    public const int PrimaryKeyMaxLength = 32;

    private static readonly Dictionary<string, string> SupportedPrimaryKeys = BuildSupportedPrimaryKeys();

    private HotkeyCombination(HotkeyModifiers modifiers, string primaryKey, string normalizedText)
    {
        Modifiers = modifiers;
        PrimaryKey = primaryKey;
        NormalizedText = normalizedText;
    }

    public HotkeyModifiers Modifiers { get; }

    public string PrimaryKey { get; }

    public string NormalizedText { get; }

    public static HotkeyCombination Create(HotkeyModifiers modifiers, string primaryKey)
    {
        if (modifiers == HotkeyModifiers.None)
        {
            throw new DomainValidationException("At least one modifier key is required.");
        }

        if ((modifiers & ~(HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift | HotkeyModifiers.Windows)) != 0)
        {
            throw new DomainValidationException("Hotkey modifiers contain an unsupported value.");
        }

        if (string.IsNullOrWhiteSpace(primaryKey))
        {
            throw new DomainValidationException("Primary key is required.");
        }

        var lookupKey = NormalizeLookupKey(primaryKey);
        if (!SupportedPrimaryKeys.TryGetValue(lookupKey, out var normalizedPrimaryKey))
        {
            throw new DomainValidationException($"Primary key '{primaryKey.Trim()}' is not supported for global hotkeys.");
        }

        var normalizedText = BuildNormalizedText(modifiers, normalizedPrimaryKey);
        return new HotkeyCombination(modifiers, normalizedPrimaryKey, normalizedText);
    }

    public static HotkeyCombination FromPersisted(HotkeyModifiers modifiers, string primaryKey, string normalizedText)
    {
        var combination = Create(modifiers, primaryKey);
        if (!string.Equals(combination.NormalizedText, normalizedText, StringComparison.Ordinal))
        {
            throw new DomainValidationException("Persisted hotkey combination is not normalized.");
        }

        return combination;
    }

    private static string BuildNormalizedText(HotkeyModifiers modifiers, string primaryKey)
    {
        var parts = new List<string>(5);
        if (modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (modifiers.HasFlag(HotkeyModifiers.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(primaryKey);
        return string.Join('+', parts);
    }

    private static string NormalizeLookupKey(string primaryKey)
    {
        return primaryKey.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).Replace("+", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }

    private static Dictionary<string, string> BuildSupportedPrimaryKeys()
    {
        var keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var letter = 'A'; letter <= 'Z'; letter++)
        {
            keys[letter.ToString()] = letter.ToString();
        }

        for (var digit = '0'; digit <= '9'; digit++)
        {
            keys[digit.ToString()] = digit.ToString();
        }

        for (var functionKey = 1; functionKey <= 24; functionKey++)
        {
            var key = $"F{functionKey}";
            keys[key] = key;
        }

        Add(keys, "Insert");
        Add(keys, "Delete");
        Add(keys, "Home");
        Add(keys, "End");
        Add(keys, "PageUp", "Prior");
        Add(keys, "PageDown", "Next");
        Add(keys, "ArrowUp", "Up");
        Add(keys, "ArrowDown", "Down");
        Add(keys, "ArrowLeft", "Left");
        Add(keys, "ArrowRight", "Right");

        return keys;
    }

    private static void Add(Dictionary<string, string> keys, string canonical, params string[] aliases)
    {
        keys[NormalizeLookupKey(canonical)] = canonical;
        foreach (var alias in aliases)
        {
            keys[NormalizeLookupKey(alias)] = canonical;
        }
    }
}

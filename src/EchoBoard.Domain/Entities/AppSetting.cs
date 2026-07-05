namespace EchoBoard.Domain.Entities;

public sealed class AppSetting
{
    public const int KeyMaxLength = 160;
    public const int ValueMaxLength = 2048;

    private AppSetting()
    {
        Key = string.Empty;
        Value = string.Empty;
    }

    private AppSetting(string key, string value, DateTimeOffset updatedAt)
    {
        Key = ValidateKey(key);
        Value = ValidateValue(value);
        UpdatedAt = ValidateUtc(updatedAt);
    }

    public string Key { get; private set; }

    public string Value { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static AppSetting Create(string key, string value, DateTimeOffset updatedAt)
    {
        return new AppSetting(key, value, updatedAt);
    }

    public void ChangeValue(string value, DateTimeOffset updatedAt)
    {
        Value = ValidateValue(value);
        UpdatedAt = ValidateUtc(updatedAt);
    }

    private static string ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Setting key is required.", nameof(key));
        }

        var trimmed = key.Trim();
        if (trimmed.Length > KeyMaxLength)
        {
            throw new ArgumentException($"Setting key must be {KeyMaxLength} characters or less.", nameof(key));
        }

        return trimmed;
    }

    private static string ValidateValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length > ValueMaxLength)
        {
            throw new ArgumentException($"Setting value must be {ValueMaxLength} characters or less.", nameof(value));
        }

        return value;
    }

    private static DateTimeOffset ValidateUtc(DateTimeOffset updatedAt)
    {
        if (updatedAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("UpdatedAt must be a UTC timestamp.", nameof(updatedAt));
        }

        return updatedAt;
    }
}

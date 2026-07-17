using EchoBoard.Domain.Exceptions;

namespace EchoBoard.Domain.Entities;

public sealed class Sound
{
    public const int NameMaxLength = 120;
    public const int FilePathMaxLength = 1024;
    public const int ExtensionMaxLength = 10;
    public const double MinVolume = 0.0;
    public const double MaxVolume = 1.0;
    public const double DefaultVolume = 1.0;

    public static readonly IReadOnlySet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3",
        ".wav",
        ".ogg",
        ".flac",
        ".m4a",
        ".aac"
    };

    private Sound()
    {
        Name = string.Empty;
        FilePath = string.Empty;
        Extension = string.Empty;
    }

    private Sound(
        Guid id,
        string name,
        string filePath,
        string extension,
        TimeSpan duration,
        long fileSize,
        double volume,
        bool isFavorite,
        bool isLoopEnabled,
        bool stopPreviousSound,
        bool allowOverlap,
        byte[] waveformPeaks,
        Guid? categoryId,
        int sortOrder,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        Name = name;
        FilePath = filePath;
        Extension = extension;
        Duration = duration;
        FileSize = fileSize;
        Volume = volume;
        IsFavorite = isFavorite;
        IsLoopEnabled = isLoopEnabled;
        StopPreviousSound = stopPreviousSound;
        AllowOverlap = allowOverlap;
        WaveformPeaks = waveformPeaks;
        CategoryId = categoryId;
        SortOrder = sortOrder;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string FilePath { get; private set; }

    public string Extension { get; private set; }

    public TimeSpan Duration { get; private set; }

    public long FileSize { get; private set; }

    public double Volume { get; private set; }

    public bool IsFavorite { get; private set; }

    public bool IsLoopEnabled { get; private set; }

    public bool StopPreviousSound { get; private set; }

    public bool AllowOverlap { get; private set; }

    public byte[] WaveformPeaks { get; private set; } = [];

    public Guid? CategoryId { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Sound Create(
        string name,
        string filePath,
        string extension,
        TimeSpan duration,
        long fileSize,
        Guid? categoryId,
        int sortOrder,
        DateTimeOffset createdAt,
        byte[]? waveformPeaks = null)
    {
        var utcCreatedAt = ValidateUtc(createdAt, nameof(createdAt));

        return new Sound(
            Guid.NewGuid(),
            ValidateName(name),
            ValidateFilePath(filePath),
            ValidateExtension(extension),
            ValidateDuration(duration),
            ValidateFileSize(fileSize),
            DefaultVolume,
            isFavorite: false,
            isLoopEnabled: false,
            stopPreviousSound: true,
            allowOverlap: false,
            ValidateWaveformPeaks(waveformPeaks),
            categoryId,
            ValidateSortOrder(sortOrder),
            utcCreatedAt,
            utcCreatedAt);
    }

    public void Rename(string name, DateTimeOffset updatedAt)
    {
        Name = ValidateName(name);
        UpdatedAt = ValidateUtc(updatedAt, nameof(updatedAt));
    }

    public void MoveToCategory(Guid categoryId, DateTimeOffset updatedAt)
    {
        CategoryId = categoryId;
        UpdatedAt = ValidateUtc(updatedAt, nameof(updatedAt));
    }

    public void ClearCategory(DateTimeOffset updatedAt)
    {
        CategoryId = null;
        UpdatedAt = ValidateUtc(updatedAt, nameof(updatedAt));
    }

    public void ChangeSortOrder(int sortOrder, DateTimeOffset updatedAt)
    {
        SortOrder = ValidateSortOrder(sortOrder);
        UpdatedAt = ValidateUtc(updatedAt, nameof(updatedAt));
    }

    public void ChangeVolume(double volume, DateTimeOffset updatedAt)
    {
        Volume = ValidateVolume(volume);
        UpdatedAt = ValidateUtc(updatedAt, nameof(updatedAt));
    }

    public void SetFavorite(bool isFavorite, DateTimeOffset updatedAt)
    {
        IsFavorite = isFavorite;
        UpdatedAt = ValidateUtc(updatedAt, nameof(updatedAt));
    }

    public void ConfigurePlayback(bool isLoopEnabled, bool stopPreviousSound, bool allowOverlap, DateTimeOffset updatedAt)
    {
        IsLoopEnabled = isLoopEnabled;
        StopPreviousSound = stopPreviousSound;
        AllowOverlap = allowOverlap;
        UpdatedAt = ValidateUtc(updatedAt, nameof(updatedAt));
    }

    public void SetWaveformPeaks(byte[] waveformPeaks, DateTimeOffset updatedAt)
    {
        WaveformPeaks = ValidateWaveformPeaks(waveformPeaks);
        UpdatedAt = ValidateUtc(updatedAt, nameof(updatedAt));
    }

    public void UpdateFileMetadata(string extension, TimeSpan duration, long fileSize, DateTimeOffset updatedAt)
    {
        Extension = ValidateExtension(extension);
        Duration = ValidateDuration(duration);
        FileSize = ValidateFileSize(fileSize);
        UpdatedAt = ValidateUtc(updatedAt, nameof(updatedAt));
    }

    public void ChangeFilePath(string filePath, DateTimeOffset updatedAt)
    {
        FilePath = ValidateFilePath(filePath);
        UpdatedAt = ValidateUtc(updatedAt, nameof(updatedAt));
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainValidationException($"Name must be {NameMaxLength} characters or less.");
        }

        return trimmed;
    }

    private static string ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new DomainValidationException("FilePath is required.");
        }

        var trimmed = filePath.Trim();
        if (trimmed.Length > FilePathMaxLength)
        {
            throw new DomainValidationException($"FilePath must be {FilePathMaxLength} characters or less.");
        }

        return trimmed;
    }

    private static string ValidateExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new DomainValidationException("Extension is required.");
        }

        var trimmed = extension.Trim();
        var normalized = trimmed.StartsWith('.') ? trimmed.ToLowerInvariant() : $".{trimmed.ToLowerInvariant()}";
        if (normalized.Length > ExtensionMaxLength)
        {
            throw new DomainValidationException($"Extension must be {ExtensionMaxLength} characters or less.");
        }

        if (!AllowedExtensions.Contains(normalized))
        {
            throw new DomainValidationException("The sound file extension is not supported.");
        }

        return normalized;
    }

    private static TimeSpan ValidateDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new DomainValidationException("Duration must be zero or greater.");
        }

        return duration;
    }

    private static long ValidateFileSize(long fileSize)
    {
        if (fileSize <= 0)
        {
            throw new DomainValidationException("FileSize must be greater than zero.");
        }

        return fileSize;
    }

    private static double ValidateVolume(double volume)
    {
        if (volume is < MinVolume or > MaxVolume)
        {
            throw new DomainValidationException("Volume must be between 0.0 and 1.0.");
        }

        return volume;
    }

    private static int ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new DomainValidationException("SortOrder must be zero or greater.");
        }

        return sortOrder;
    }

    private static byte[] ValidateWaveformPeaks(byte[]? waveformPeaks)
    {
        if (waveformPeaks is null || waveformPeaks.Length == 0)
        {
            return [];
        }

        if (waveformPeaks.Length != 32)
        {
            throw new DomainValidationException("WaveformPeaks must contain exactly 32 values.");
        }

        return [.. waveformPeaks];
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

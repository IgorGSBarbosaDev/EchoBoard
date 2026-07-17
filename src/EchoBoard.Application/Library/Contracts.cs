using EchoBoard.Domain.Entities;

namespace EchoBoard.Application.Library;

public sealed record SoundDto(
    Guid Id,
    string Name,
    string FilePath,
    string Extension,
    TimeSpan Duration,
    long FileSize,
    double Volume,
    bool IsFavorite,
    Guid? CategoryId,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsLoopEnabled,
    bool StopPreviousSound,
    bool AllowOverlap,
    byte[] WaveformPeaks);

public sealed record CategoryDto(Guid Id, string Name, int SortOrder, DateTimeOffset CreatedAt);

public sealed record SoundLibraryFilter(
    string? SearchText,
    Guid? CategoryId,
    bool IncludeUncategorizedOnly,
    bool FavoritesOnly)
{
    public static SoundLibraryFilter All { get; } = new(null, null, IncludeUncategorizedOnly: false, FavoritesOnly: false);
}

public sealed record SoundLibraryItemDto(
    Guid Id,
    string Name,
    string FilePath,
    string Extension,
    TimeSpan Duration,
    long FileSize,
    double Volume,
    bool IsFavorite,
    Guid? CategoryId,
    string? CategoryName,
    int? CategorySortOrder,
    int SortOrder,
    bool IsMissingFile,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsLoopEnabled,
    bool StopPreviousSound,
    bool AllowOverlap,
    byte[] WaveformPeaks,
    int PlayCount);

public sealed record SoundLibraryCategoryDto(
    Guid Id,
    string Name,
    int SortOrder,
    int SoundCount,
    DateTimeOffset CreatedAt);

public sealed record SoundLibraryResult(
    IReadOnlyList<SoundLibraryItemDto> Sounds,
    IReadOnlyList<SoundLibraryCategoryDto> Categories,
    int TotalSoundCount,
    int UncategorizedSoundCount,
    string ActiveFilterSummary);

public sealed record CreateSoundRequest(
    string Name,
    string FilePath,
    string Extension,
    TimeSpan Duration,
    long FileSize,
    Guid? CategoryId,
    int SortOrder,
    DateTimeOffset CreatedAt);

public sealed record UpdateSoundRequest(
    Guid Id,
    string Name,
    string FilePath,
    string Extension,
    TimeSpan Duration,
    long FileSize,
    double Volume,
    bool IsFavorite,
    Guid? CategoryId,
    int SortOrder,
    DateTimeOffset UpdatedAt,
    bool IsLoopEnabled = false,
    bool StopPreviousSound = true,
    bool AllowOverlap = false,
    byte[]? WaveformPeaks = null);

public sealed record SetSoundFavoriteRequest(Guid Id, bool IsFavorite, DateTimeOffset UpdatedAt);

public sealed record AssignSoundCategoryRequest(Guid Id, Guid? CategoryId, DateTimeOffset UpdatedAt);

public sealed record CreateCategoryRequest(string Name, int SortOrder, DateTimeOffset CreatedAt);

public sealed record UpdateCategoryRequest(Guid Id, string Name, int SortOrder);

public sealed record ImportSoundsRequest(IReadOnlyList<string> FilePaths, DateTimeOffset ImportedAt);

public sealed record ImportSoundsResult(IReadOnlyList<ImportSoundItemResult> Items);

public sealed record ImportSoundItemResult(string FilePath, ImportSoundStatus Status, string Message, SoundDto? Sound);

public sealed record AudioFileMetadata(
    string DisplayName,
    string FullPath,
    string Extension,
    TimeSpan Duration,
    long FileSize,
    byte[]? WaveformPeaks = null);

public sealed record RecentlyPlayedDto(Guid Id, Guid SoundId, DateTimeOffset PlayedAt);

public enum ImportSoundStatus
{
    Imported,
    SkippedDuplicate,
    InvalidExtension,
    Unreadable,
    MetadataFailed
}

public interface IAudioFileMetadataReader
{
    Task<AudioFileMetadata> ReadAsync(string filePath, CancellationToken cancellationToken);
}

public interface ISoundFileAvailabilityReader
{
    Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken);
}

public interface ISoundLibraryRepository
{
    Task<IReadOnlyList<Sound>> ListSoundsAsync(CancellationToken cancellationToken);

    Task<Sound?> GetSoundAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> SoundFilePathExistsAsync(string filePath, Guid? excludingSoundId, CancellationToken cancellationToken);

    Task AddSoundAsync(Sound sound, CancellationToken cancellationToken);

    Task UpdateSoundAsync(Sound sound, CancellationToken cancellationToken);

    Task DeleteSoundAsync(Guid id, CancellationToken cancellationToken);
}

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListCategoriesAsync(CancellationToken cancellationToken);

    Task<Category?> GetCategoryAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> CategoryNameExistsAsync(string name, Guid? excludingCategoryId, CancellationToken cancellationToken);

    Task AddCategoryAsync(Category category, CancellationToken cancellationToken);

    Task UpdateCategoryAsync(Category category, CancellationToken cancellationToken);

    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken);
}

public interface IRecentlyPlayedRepository
{
    Task AddAsync(RecentlyPlayed entry, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, int>> GetPlayCountsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<RecentlyPlayed>> ListAsync(int limit, CancellationToken cancellationToken);
}

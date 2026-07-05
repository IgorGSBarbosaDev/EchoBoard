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
    DateTimeOffset UpdatedAt);

public sealed record CategoryDto(Guid Id, string Name, int SortOrder, DateTimeOffset CreatedAt);

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
    DateTimeOffset UpdatedAt);

public sealed record CreateCategoryRequest(string Name, int SortOrder, DateTimeOffset CreatedAt);

public sealed record UpdateCategoryRequest(Guid Id, string Name, int SortOrder);

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

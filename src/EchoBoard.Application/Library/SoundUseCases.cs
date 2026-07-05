using EchoBoard.Domain.Entities;

namespace EchoBoard.Application.Library;

public sealed class CreateSoundUseCase
{
    private readonly ISoundLibraryRepository sounds;
    private readonly ICategoryRepository categories;

    public CreateSoundUseCase(ISoundLibraryRepository sounds, ICategoryRepository categories)
    {
        this.sounds = sounds;
        this.categories = categories;
    }

    public async Task<SoundDto> ExecuteAsync(CreateSoundRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
        await EnsureSoundPathIsUniqueAsync(request.FilePath, excludingSoundId: null, cancellationToken);

        var sound = Sound.Create(
            request.Name,
            PathNormalizer.NormalizeFilePath(request.FilePath),
            request.Extension,
            request.Duration,
            request.FileSize,
            request.CategoryId,
            request.SortOrder,
            request.CreatedAt);

        await sounds.AddSoundAsync(sound, cancellationToken);

        return LibraryMapper.ToDto(sound);
    }

    private async Task EnsureCategoryExistsAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId is null)
        {
            return;
        }

        var category = await categories.GetCategoryAsync(categoryId.Value, cancellationToken);
        if (category is null)
        {
            throw new CategoryNotFoundException(categoryId.Value);
        }
    }

    private async Task EnsureSoundPathIsUniqueAsync(string filePath, Guid? excludingSoundId, CancellationToken cancellationToken)
    {
        var normalizedPath = PathNormalizer.NormalizeFilePath(filePath);
        if (await sounds.SoundFilePathExistsAsync(normalizedPath, excludingSoundId, cancellationToken))
        {
            throw new DuplicateSoundFilePathException(normalizedPath);
        }
    }
}

public sealed class GetSoundUseCase
{
    private readonly ISoundLibraryRepository sounds;

    public GetSoundUseCase(ISoundLibraryRepository sounds)
    {
        this.sounds = sounds;
    }

    public async Task<SoundDto> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var sound = await sounds.GetSoundAsync(id, cancellationToken);

        return sound is null ? throw new SoundNotFoundException(id) : LibraryMapper.ToDto(sound);
    }
}

public sealed class ListSoundsUseCase
{
    private readonly ISoundLibraryRepository sounds;

    public ListSoundsUseCase(ISoundLibraryRepository sounds)
    {
        this.sounds = sounds;
    }

    public async Task<IReadOnlyList<SoundDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var allSounds = await sounds.ListSoundsAsync(cancellationToken);

        return allSounds.Select(LibraryMapper.ToDto).ToArray();
    }
}

public sealed class UpdateSoundUseCase
{
    private readonly ISoundLibraryRepository sounds;
    private readonly ICategoryRepository categories;

    public UpdateSoundUseCase(ISoundLibraryRepository sounds, ICategoryRepository categories)
    {
        this.sounds = sounds;
        this.categories = categories;
    }

    public async Task<SoundDto> ExecuteAsync(UpdateSoundRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sound = await sounds.GetSoundAsync(request.Id, cancellationToken);
        if (sound is null)
        {
            throw new SoundNotFoundException(request.Id);
        }

        if (request.CategoryId is not null && await categories.GetCategoryAsync(request.CategoryId.Value, cancellationToken) is null)
        {
            throw new CategoryNotFoundException(request.CategoryId.Value);
        }

        var normalizedPath = PathNormalizer.NormalizeFilePath(request.FilePath);
        if (await sounds.SoundFilePathExistsAsync(normalizedPath, request.Id, cancellationToken))
        {
            throw new DuplicateSoundFilePathException(normalizedPath);
        }

        sound.Rename(request.Name, request.UpdatedAt);
        sound.ChangeFilePath(normalizedPath, request.UpdatedAt);
        sound.UpdateFileMetadata(request.Extension, request.Duration, request.FileSize, request.UpdatedAt);
        sound.ChangeVolume(request.Volume, request.UpdatedAt);
        sound.SetFavorite(request.IsFavorite, request.UpdatedAt);
        sound.ChangeSortOrder(request.SortOrder, request.UpdatedAt);

        if (request.CategoryId is null)
        {
            sound.ClearCategory(request.UpdatedAt);
        }
        else
        {
            sound.MoveToCategory(request.CategoryId.Value, request.UpdatedAt);
        }

        await sounds.UpdateSoundAsync(sound, cancellationToken);

        return LibraryMapper.ToDto(sound);
    }
}

public sealed class DeleteSoundUseCase
{
    private readonly ISoundLibraryRepository sounds;

    public DeleteSoundUseCase(ISoundLibraryRepository sounds)
    {
        this.sounds = sounds;
    }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await sounds.GetSoundAsync(id, cancellationToken) is null)
        {
            throw new SoundNotFoundException(id);
        }

        await sounds.DeleteSoundAsync(id, cancellationToken);
    }
}

using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Exceptions;
using EchoBoard.Application.Hotkeys;

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

    public UpdateSoundUseCase(ISoundLibraryRepository sounds)
    {
        this.sounds = sounds;
    }

    public async Task<SoundDto> ExecuteAsync(UpdateSoundRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sound = await sounds.GetSoundAsync(request.Id, cancellationToken);
        if (sound is null)
        {
            throw new SoundNotFoundException(request.Id);
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
        sound.ConfigurePlayback(request.IsLoopEnabled, request.StopPreviousSound, request.AllowOverlap, request.UpdatedAt);
        if (request.WaveformPeaks is { Length: > 0 })
        {
            sound.SetWaveformPeaks(request.WaveformPeaks, request.UpdatedAt);
        }
        sound.ChangeSortOrder(request.SortOrder, request.UpdatedAt);

        await sounds.UpdateSoundAsync(sound, cancellationToken);

        return LibraryMapper.ToDto(sound);
    }
}

public sealed class SetSoundFavoriteUseCase
{
    private readonly ISoundLibraryRepository sounds;

    public SetSoundFavoriteUseCase(ISoundLibraryRepository sounds)
    {
        this.sounds = sounds;
    }

    public async Task<SoundDto> ExecuteAsync(SetSoundFavoriteRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sound = await sounds.GetSoundAsync(request.Id, cancellationToken);
        if (sound is null)
        {
            throw new SoundNotFoundException(request.Id);
        }

        sound.SetFavorite(request.IsFavorite, request.UpdatedAt);
        await sounds.UpdateSoundAsync(sound, cancellationToken);

        return LibraryMapper.ToDto(sound);
    }
}

public sealed class SetCategorySoundsUseCase
{
    private readonly ISoundLibraryRepository sounds;
    private readonly ICategoryRepository categories;

    public SetCategorySoundsUseCase(
        ISoundLibraryRepository sounds,
        ICategoryRepository categories)
    {
        this.sounds = sounds;
        this.categories = categories;
    }

    public async Task ExecuteAsync(
        Guid categoryId,
        IReadOnlyCollection<Guid> soundIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(soundIds);
        if (await categories.GetCategoryAsync(categoryId, cancellationToken) is null)
        {
            throw new CategoryNotFoundException(categoryId);
        }

        var uniqueSoundIds = soundIds.ToHashSet();
        if (uniqueSoundIds.Count != soundIds.Count)
        {
            throw new DomainValidationException("A sound can only appear once in a category.");
        }

        if (uniqueSoundIds.Contains(Guid.Empty))
        {
            throw new DomainValidationException("A sound id cannot be empty.");
        }

        var soundsInCategory = await sounds.GetSoundsByCategoryIdAsync(categoryId, cancellationToken);
        IReadOnlyList<Sound> selectedSounds = uniqueSoundIds.Count == 0
            ? []
            : await sounds.GetSoundsByIdsAsync(uniqueSoundIds, cancellationToken);
        var selectedSoundIds = selectedSounds.Select(sound => sound.Id).ToHashSet();
        var missingSoundId = uniqueSoundIds.FirstOrDefault(soundId => !selectedSoundIds.Contains(soundId));
        if (missingSoundId != Guid.Empty)
        {
            throw new SoundNotFoundException(missingSoundId);
        }

        var updatedSounds = new List<Sound>(selectedSounds.Count + soundsInCategory.Count);
        var updatedAt = DateTimeOffset.UtcNow;
        foreach (var sound in selectedSounds)
        {
            if (sound.AssignToCategory(categoryId, updatedAt))
            {
                updatedSounds.Add(sound);
            }
        }

        foreach (var sound in soundsInCategory.Where(sound => !uniqueSoundIds.Contains(sound.Id)))
        {
            if (sound.RemoveFromCategory(categoryId, updatedAt))
            {
                updatedSounds.Add(sound);
            }
        }

        await sounds.UpdateSoundsAsync(updatedSounds, cancellationToken);
    }
}

public sealed class DeleteSoundUseCase
{
    private readonly ISoundLibraryRepository sounds;
    private readonly IHotkeyBindingRepository? hotkeys;
    private readonly IHotkeyRuntimeService? hotkeyRuntime;

    public DeleteSoundUseCase(
        ISoundLibraryRepository sounds,
        IHotkeyBindingRepository? hotkeys = null,
        IHotkeyRuntimeService? hotkeyRuntime = null)
    {
        this.sounds = sounds;
        this.hotkeys = hotkeys;
        this.hotkeyRuntime = hotkeyRuntime;
    }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await sounds.GetSoundAsync(id, cancellationToken) is null)
        {
            throw new SoundNotFoundException(id);
        }

        var binding = hotkeys is null ? null : await hotkeys.GetForSoundAsync(id, cancellationToken);
        if (binding is not null && hotkeyRuntime is not null)
        {
            await hotkeyRuntime.UnregisterBindingAsync(binding.Id, cancellationToken);
        }

        try
        {
            await sounds.DeleteSoundAsync(id, cancellationToken);
        }
        catch
        {
            if (binding is { IsEnabled: true } && hotkeyRuntime is not null)
            {
                await hotkeyRuntime.RegisterBindingAsync(binding, CancellationToken.None);
            }
            throw;
        }
    }
}

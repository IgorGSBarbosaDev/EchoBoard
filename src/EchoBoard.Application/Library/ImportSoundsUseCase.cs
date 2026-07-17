using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Exceptions;

namespace EchoBoard.Application.Library;

public sealed class ImportSoundsUseCase
{
    private readonly ISoundLibraryRepository sounds;
    private readonly IAudioFileMetadataReader metadataReader;

    public ImportSoundsUseCase(ISoundLibraryRepository sounds, IAudioFileMetadataReader metadataReader)
    {
        this.sounds = sounds;
        this.metadataReader = metadataReader;
    }

    public async Task<ImportSoundsResult> ExecuteAsync(ImportSoundsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var importedAt = EnsureUtc(request.ImportedAt);
        var nextSortOrder = await GetNextSortOrderAsync(cancellationToken);
        var results = new List<ImportSoundItemResult>(request.FilePaths.Count);

        foreach (var filePath in request.FilePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await ImportOneAsync(filePath, nextSortOrder, importedAt, cancellationToken);
            results.Add(result);

            if (result.Status == ImportSoundStatus.Imported)
            {
                nextSortOrder++;
            }
        }

        return new ImportSoundsResult(results);
    }

    private async Task<ImportSoundItemResult> ImportOneAsync(
        string filePath,
        int sortOrder,
        DateTimeOffset importedAt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Failed(filePath, ImportSoundStatus.Unreadable, "File path is required.");
        }

        var normalizedPath = PathNormalizer.NormalizeFilePath(filePath);
        var extension = Path.GetExtension(normalizedPath);
        if (!Sound.AllowedExtensions.Contains(extension))
        {
            return Failed(normalizedPath, ImportSoundStatus.InvalidExtension, "Supported formats are MP3, WAV, OGG, FLAC, M4A, and AAC.");
        }

        if (await sounds.SoundFilePathExistsAsync(normalizedPath, excludingSoundId: null, cancellationToken))
        {
            return Failed(normalizedPath, ImportSoundStatus.SkippedDuplicate, "This file is already in the library.");
        }

        AudioFileMetadata metadata;
        try
        {
            metadata = await metadataReader.ReadAsync(normalizedPath, cancellationToken);
        }
        catch (AudioFileUnreadableException exception)
        {
            return Failed(normalizedPath, ImportSoundStatus.Unreadable, exception.Message);
        }
        catch (AudioFileMetadataException exception)
        {
            return Failed(normalizedPath, ImportSoundStatus.MetadataFailed, exception.Message);
        }

        try
        {
            var sound = Sound.Create(
                metadata.DisplayName,
                metadata.FullPath,
                metadata.Extension,
                metadata.Duration,
                metadata.FileSize,
                categoryId: null,
                sortOrder,
                importedAt);

            await sounds.AddSoundAsync(sound, cancellationToken);

            return new ImportSoundItemResult(
                normalizedPath,
                ImportSoundStatus.Imported,
                "Imported successfully.",
                LibraryMapper.ToDto(sound));
        }
        catch (DuplicateSoundFilePathException)
        {
            return Failed(normalizedPath, ImportSoundStatus.SkippedDuplicate, "This file is already in the library.");
        }
        catch (DomainValidationException exception)
        {
            return Failed(normalizedPath, ImportSoundStatus.MetadataFailed, exception.Message);
        }
    }

    private async Task<int> GetNextSortOrderAsync(CancellationToken cancellationToken)
    {
        var existingSounds = await sounds.ListSoundsAsync(cancellationToken);

        return existingSounds.Count == 0 ? 0 : existingSounds.Max(sound => sound.SortOrder) + 1;
    }

    private static ImportSoundItemResult Failed(string filePath, ImportSoundStatus status, string message)
    {
        return new ImportSoundItemResult(filePath, status, message, Sound: null);
    }

    private static DateTimeOffset EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("ImportedAt must be a UTC timestamp.", nameof(value));
        }

        return value;
    }
}

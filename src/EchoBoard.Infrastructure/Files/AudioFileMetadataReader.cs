using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using NAudio.Wave;
using System.Runtime.InteropServices;
using System.Security;

namespace EchoBoard.Infrastructure.Files;

public sealed class AudioFileMetadataReader : IAudioFileMetadataReader
{
    public Task<AudioFileMetadata> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedPath = PathNormalizer.NormalizeFilePath(filePath);
        var extension = Path.GetExtension(normalizedPath).ToLowerInvariant();
        if (!Sound.AllowedExtensions.Contains(extension))
        {
            throw new AudioFileMetadataException(normalizedPath, "Only MP3 and WAV files can be imported.");
        }

        var fileInfo = new FileInfo(normalizedPath);
        if (!fileInfo.Exists)
        {
            throw new AudioFileUnreadableException(normalizedPath, "The file does not exist.");
        }

        if (fileInfo.Length <= 0)
        {
            throw new AudioFileMetadataException(normalizedPath, "The audio file is empty.");
        }

        try
        {
            using var stream = new FileStream(
                normalizedPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            throw new AudioFileUnreadableException(normalizedPath, "The file cannot be read.");
        }

        TimeSpan duration;
        try
        {
            using var reader = new AudioFileReader(normalizedPath);
            duration = reader.TotalTime;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or COMException or NotSupportedException or ArgumentException)
        {
            throw new AudioFileMetadataException(normalizedPath, "Audio metadata could not be read.");
        }

        if (duration <= TimeSpan.Zero)
        {
            throw new AudioFileMetadataException(normalizedPath, "Audio duration could not be read.");
        }

        var displayName = Path.GetFileNameWithoutExtension(normalizedPath);

        return Task.FromResult(new AudioFileMetadata(
            displayName,
            normalizedPath,
            extension,
            duration,
            fileInfo.Length));
    }
}

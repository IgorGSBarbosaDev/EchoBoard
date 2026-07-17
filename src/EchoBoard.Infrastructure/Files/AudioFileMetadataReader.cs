using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using NAudio.Wave;
using NAudio.Vorbis;
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
            throw new AudioFileMetadataException(normalizedPath, "The audio format is not supported.");
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
        byte[] waveformPeaks;
        try
        {
            using var reader = OpenAudioReader(normalizedPath, extension);
            duration = GetDuration(reader);
            waveformPeaks = ExtractWaveform(reader, 32, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or COMException or NotSupportedException or ArgumentException)
        {
            throw new AudioFileMetadataException(normalizedPath, "The file is corrupted or uses an unsupported audio encoding.");
        }

        if (duration <= TimeSpan.Zero)
        {
            throw new AudioFileMetadataException(normalizedPath, "The file is corrupted or its audio duration could not be determined.");
        }

        var displayName = Path.GetFileNameWithoutExtension(normalizedPath);

        return Task.FromResult(new AudioFileMetadata(
            displayName,
            normalizedPath,
            extension,
            duration,
            fileInfo.Length,
            waveformPeaks));
    }

    private static WaveStream OpenAudioReader(string filePath, string extension)
    {
        if (string.Equals(extension, ".ogg", StringComparison.OrdinalIgnoreCase))
        {
            return new VorbisWaveReader(filePath);
        }

        if (string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return new AudioFileReader(filePath);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or COMException or NotSupportedException or ArgumentException)
            {
                return new Mp3FileReader(filePath);
            }
        }

        return new AudioFileReader(filePath);
    }

    private static TimeSpan GetDuration(WaveStream reader)
    {
        if (reader.TotalTime > TimeSpan.Zero)
        {
            return reader.TotalTime;
        }

        return reader.WaveFormat.AverageBytesPerSecond > 0 && reader.Length > 0
            ? TimeSpan.FromSeconds((double)reader.Length / reader.WaveFormat.AverageBytesPerSecond)
            : TimeSpan.Zero;
    }

    private static byte[] ExtractWaveform(WaveStream reader, int peakCount, CancellationToken cancellationToken)
    {
        var provider = reader.ToSampleProvider();
        var totalSamples = Math.Max(
            1L,
            (long)Math.Ceiling(reader.TotalTime.TotalSeconds * provider.WaveFormat.SampleRate * provider.WaveFormat.Channels));
        var samplesPerPeak = Math.Max(1L, totalSamples / peakCount);
        var maxima = new float[peakCount];
        var buffer = new float[8192];
        long sampleIndex = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = provider.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            for (var index = 0; index < read; index++, sampleIndex++)
            {
                var peakIndex = (int)Math.Min(peakCount - 1, sampleIndex / samplesPerPeak);
                maxima[peakIndex] = Math.Max(maxima[peakIndex], Math.Abs(buffer[index]));
            }
        }

        return maxima
            .Select(value => (byte)Math.Clamp((int)Math.Round(value * byte.MaxValue), 0, byte.MaxValue))
            .ToArray();
    }
}

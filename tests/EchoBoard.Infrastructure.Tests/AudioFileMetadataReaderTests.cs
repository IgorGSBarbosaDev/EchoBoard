using EchoBoard.Application.Library;
using EchoBoard.Infrastructure.Files;
using FluentAssertions;
using Xunit;

namespace EchoBoard.Infrastructure.Tests;

public sealed class AudioFileMetadataReaderTests
{
    [Fact]
    public async Task ReadAsyncExtractsMetadataFromWavFile()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"echoboard-{Guid.NewGuid():N}.wav");
        try
        {
            WriteSilentWav(filePath, TimeSpan.FromMilliseconds(250));
            var reader = new AudioFileMetadataReader();

            var metadata = await reader.ReadAsync(filePath, TestContext.Current.CancellationToken);

            metadata.DisplayName.Should().Be(Path.GetFileNameWithoutExtension(filePath));
            metadata.FullPath.Should().Be(Path.GetFullPath(filePath));
            metadata.Extension.Should().Be(".wav");
            metadata.FileSize.Should().BeGreaterThan(0);
            metadata.Duration.Should().BeCloseTo(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(25));
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task ReadAsyncRejectsMissingFile()
    {
        var reader = new AudioFileMetadataReader();
        var missingPath = Path.Combine(Path.GetTempPath(), $"echoboard-{Guid.NewGuid():N}.wav");

        var act = () => reader.ReadAsync(missingPath, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AudioFileUnreadableException>();
    }

    [Fact]
    public async Task ReadAsyncRejectsUnsupportedExtension()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"echoboard-{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllTextAsync(filePath, "not audio", TestContext.Current.CancellationToken);
            var reader = new AudioFileMetadataReader();

            var act = () => reader.ReadAsync(filePath, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<AudioFileMetadataException>();
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    private static void WriteSilentWav(string filePath, TimeSpan duration)
    {
        const int sampleRate = 48000;
        const short channels = 1;
        const short bitsPerSample = 16;
        var sampleCount = (int)(sampleRate * duration.TotalSeconds);
        var dataSize = sampleCount * channels * bitsPerSample / 8;
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        short blockAlign = channels * bitsPerSample / 8;

        using var stream = File.Create(filePath);
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8);
        writer.Write(dataSize);
        writer.Write(new byte[dataSize]);
    }
}

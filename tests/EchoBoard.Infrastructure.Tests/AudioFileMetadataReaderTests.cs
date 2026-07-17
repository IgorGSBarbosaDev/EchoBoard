using EchoBoard.Application.Library;
using EchoBoard.Infrastructure.Files;
using FluentAssertions;
using NAudio.Wave;
using Xunit;

namespace EchoBoard.Infrastructure.Tests;

public sealed class AudioFileMetadataReaderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadAsyncAcceptsMp3WithOrWithoutId3Metadata(bool includeId3Header)
    {
        var wavPath = TemporaryFilePath(".wav");
        var mp3Path = TemporaryFilePath(".mp3");
        try
        {
            WriteSilentWav(wavPath, TimeSpan.FromMilliseconds(500));
            using (var source = new WaveFileReader(wavPath))
            {
                MediaFoundationEncoder.EncodeToMp3(source, mp3Path, 128_000);
            }

            if (includeId3Header)
            {
                var audioBytes = await File.ReadAllBytesAsync(mp3Path, TestContext.Current.CancellationToken);
                byte[] id3Header =
                [
                    (byte)'I', (byte)'D', (byte)'3', 3, 0, 0, 0, 0, 0, 15,
                    (byte)'T', (byte)'I', (byte)'T', (byte)'2', 0, 0, 0, 5, 0, 0,
                    3, (byte)'T', (byte)'e', (byte)'s', (byte)'t'
                ];
                await using var stream = File.Create(mp3Path);
                await stream.WriteAsync(id3Header, TestContext.Current.CancellationToken);
                await stream.WriteAsync(audioBytes, TestContext.Current.CancellationToken);
            }

            var metadata = await new AudioFileMetadataReader().ReadAsync(mp3Path, TestContext.Current.CancellationToken);

            metadata.DisplayName.Should().Be(Path.GetFileNameWithoutExtension(mp3Path));
            metadata.Extension.Should().Be(".mp3");
            metadata.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        }
        finally
        {
            DeleteIfExists(wavPath);
            DeleteIfExists(mp3Path);
        }
    }

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

    [Fact]
    public async Task ReadAsyncRejectsCorruptedSupportedFileWithClearMessage()
    {
        var filePath = TemporaryFilePath(".MP3");
        try
        {
            await File.WriteAllTextAsync(filePath, "not an audio stream", TestContext.Current.CancellationToken);

            var act = () => new AudioFileMetadataReader().ReadAsync(filePath, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<AudioFileMetadataException>()
                .WithMessage("*corrupted*unsupported*");
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    private static string TemporaryFilePath(string extension)
    {
        return Path.Combine(Path.GetTempPath(), $"echoboard-{Guid.NewGuid():N}{extension}");
    }

    private static void DeleteIfExists(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
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

using System.Runtime.InteropServices;
using Concentus;
using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace EchoBoard.Audio.Decoding;

internal sealed record DecodedAudio(
    string FilePath,
    string Codec,
    float[] Samples,
    TimeSpan Duration)
{
    public const int SampleRate = 48000;
    public const int Channels = 2;
}

internal static class AudioFileDecoder
{
    public static DecodedAudio Decode(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedPath = PathNormalizer.NormalizeFilePath(filePath);
        var fileInfo = new FileInfo(normalizedPath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("The audio file could not be found.", normalizedPath);
        }

        if (fileInfo.Length <= 0)
        {
            throw new InvalidDataException("The audio file is empty.");
        }

        var signature = ReadSignature(normalizedPath);
        try
        {
            var decoded = IsOggOpus(signature)
                ? DecodeOggOpus(normalizedPath, signature, cancellationToken)
                : DecodeWithNAudio(normalizedPath, signature, cancellationToken);
            if (decoded.Samples.Length == 0 || decoded.Duration <= TimeSpan.Zero)
            {
                throw new InvalidDataException("The audio decoder produced no playable samples.");
            }

            return decoded;
        }
        catch (Exception exception) when (exception is IOException
                                           or InvalidDataException
                                           or COMException
                                           or OpusException
                                           or NotSupportedException
                                           or ArgumentException)
        {
            throw new InvalidDataException("The file is corrupted or uses an unsupported audio encoding.", exception);
        }
    }

    private static DecodedAudio DecodeOggOpus(
        string filePath,
        byte[] signature,
        CancellationToken cancellationToken)
    {
        var headerIndex = signature.AsSpan().IndexOf("OpusHead"u8);
        if (headerIndex < 0 || headerIndex + 10 > signature.Length)
        {
            throw new InvalidDataException("The Ogg Opus header is invalid.");
        }

        var channels = Math.Clamp(signature[headerIndex + 9], (byte)1, (byte)2);
        var preSkip = BitConverter.ToUInt16(signature, headerIndex + 10);
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var decoder = OpusCodecFactory.CreateDecoder(DecodedAudio.SampleRate, channels, TextWriter.Null);
        var decodedSamples = new List<float>(Math.Max(4096, checked((int)Math.Min(stream.Length * 24, int.MaxValue))));
        var packet = new List<byte>(4096);
        var output = new float[5760 * channels];
        var header = new byte[27];
        var audioPacketIndex = 0;
        long finalGranule = 0;
        while (stream.Position < stream.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stream.ReadExactly(header);
            if (!header.AsSpan(0, 4).SequenceEqual("OggS"u8) || header[4] != 0)
            {
                throw new InvalidDataException("The Ogg page header is invalid.");
            }

            var granule = BitConverter.ToInt64(header.AsSpan(6, 8));
            if (granule >= 0)
            {
                finalGranule = Math.Max(finalGranule, granule);
            }

            var segmentCount = header[26];
            var lacingValues = new byte[segmentCount];
            stream.ReadExactly(lacingValues);
            foreach (var segmentLength in lacingValues)
            {
                if (segmentLength > 0)
                {
                    var start = packet.Count;
                    packet.AddRange(new byte[segmentLength]);
                    stream.ReadExactly(CollectionsMarshal.AsSpan(packet)[start..]);
                }

                if (segmentLength == byte.MaxValue)
                {
                    continue;
                }

                var packetSpan = CollectionsMarshal.AsSpan(packet);
                if (!packetSpan.StartsWith("OpusHead"u8) && !packetSpan.StartsWith("OpusTags"u8))
                {
                    int frameCount;
                    try
                    {
                        frameCount = decoder.Decode(packetSpan, output, 5760, false);
                    }
                    catch (OpusException exception)
                    {
                        var concealmentFrames = Math.Clamp(decoder.LastPacketDuration, 120, 5760);
                        try
                        {
                            frameCount = decoder.Decode(
                                ReadOnlySpan<byte>.Empty,
                                output,
                                concealmentFrames,
                                false);
                        }
                        catch (OpusException concealmentException)
                        {
                            throw new InvalidDataException(
                                $"The Ogg Opus packet {audioPacketIndex} ({packet.Count} bytes) is invalid.",
                                new AggregateException(exception, concealmentException));
                        }
                    }

                    decodedSamples.AddRange(output.AsSpan(0, frameCount * channels).ToArray());
                    audioPacketIndex++;
                }

                packet.Clear();
            }
        }

        if (packet.Count > 0)
        {
            throw new InvalidDataException("The final Ogg packet is incomplete.");
        }

        var decodedFrameCount = decodedSamples.Count / channels;
        var targetFrames = finalGranule > preSkip
            ? checked((int)Math.Min(finalGranule - preSkip, int.MaxValue))
            : decodedFrameCount;
        var sourceFrameOffset = decodedFrameCount >= targetFrames + preSkip ? preSkip : 0;
        var availableFrames = Math.Max(0, decodedFrameCount - sourceFrameOffset);
        var expectedFrames = Math.Min(availableFrames, targetFrames);
        var stereoSamples = new float[expectedFrames * DecodedAudio.Channels];
        var sourceOffset = sourceFrameOffset * channels;
        for (var frame = 0; frame < expectedFrames; frame++)
        {
            if (channels == 1)
            {
                var value = decodedSamples[sourceOffset + frame];
                stereoSamples[frame * 2] = value;
                stereoSamples[frame * 2 + 1] = value;
            }
            else
            {
                stereoSamples[frame * 2] = decodedSamples[sourceOffset + frame * 2];
                stereoSamples[frame * 2 + 1] = decodedSamples[sourceOffset + frame * 2 + 1];
            }
        }

        var duration = TimeSpan.FromSeconds(expectedFrames / (double)DecodedAudio.SampleRate);
        return new DecodedAudio(filePath, "Ogg/Opus", stereoSamples, duration);
    }

    private static DecodedAudio DecodeWithNAudio(
        string filePath,
        byte[] signature,
        CancellationToken cancellationToken)
    {
        using var reader = OpenWaveStream(filePath, signature);
        ISampleProvider provider = reader.ToSampleProvider();
        if (provider.WaveFormat.SampleRate != DecodedAudio.SampleRate)
        {
            provider = new WdlResamplingSampleProvider(provider, DecodedAudio.SampleRate);
        }

        provider = ToStereo(provider);
        var buffer = new float[8192];
        var samples = new List<float>(Math.Max(4096, checked((int)Math.Min(reader.Length / 2, int.MaxValue))));
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = provider.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            samples.AddRange(buffer.AsSpan(0, read).ToArray());
        }

        var duration = TimeSpan.FromSeconds(
            samples.Count / (double)(DecodedAudio.SampleRate * DecodedAudio.Channels));
        return new DecodedAudio(filePath, DetectCodec(signature, filePath), [.. samples], duration);
    }

    private static WaveStream OpenWaveStream(string filePath, byte[] signature)
    {
        if (IsOggVorbis(signature))
        {
            return new VorbisWaveReader(filePath);
        }

        if (IsWave(signature))
        {
            return new WaveFileReader(filePath);
        }

        if (IsFlac(signature) || IsMp4(signature) || IsAac(signature))
        {
            return new MediaFoundationReader(filePath);
        }

        if (IsMp3(signature) || string.Equals(Path.GetExtension(filePath), ".mp3", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return new Mp3FileReader(filePath);
            }
            catch (Exception exception) when (exception is InvalidDataException or NotSupportedException or ArgumentException)
            {
                return new AudioFileReader(filePath);
            }
        }

        var extension = Path.GetExtension(filePath);
        if (extension.Equals(".flac", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".m4a", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".aac", StringComparison.OrdinalIgnoreCase))
        {
            return new MediaFoundationReader(filePath);
        }

        return new AudioFileReader(filePath);
    }

    private static ISampleProvider ToStereo(ISampleProvider provider)
    {
        return provider.WaveFormat.Channels switch
        {
            1 => new MonoToStereoSampleProvider(provider),
            2 => provider,
            _ => CreateStereoMultiplexer(provider)
        };
    }

    private static MultiplexingSampleProvider CreateStereoMultiplexer(ISampleProvider provider)
    {
        var multiplexer = new MultiplexingSampleProvider([provider], 2);
        multiplexer.ConnectInputToOutput(0, 0);
        multiplexer.ConnectInputToOutput(1, 1);
        return multiplexer;
    }

    private static byte[] ReadSignature(string filePath)
    {
        var signature = new byte[128];
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var read = stream.Read(signature, 0, signature.Length);
        return signature.AsSpan(0, read).ToArray();
    }

    private static bool IsOggOpus(byte[] signature) =>
        signature.AsSpan().StartsWith("OggS"u8) && signature.AsSpan().IndexOf("OpusHead"u8) >= 0;

    private static bool IsOggVorbis(byte[] signature) =>
        signature.AsSpan().StartsWith("OggS"u8) && signature.AsSpan().IndexOf("vorbis"u8) >= 0;

    private static bool IsWave(byte[] signature) =>
        signature.AsSpan().StartsWith("RIFF"u8) && signature.AsSpan().IndexOf("WAVE"u8) >= 0;

    private static bool IsMp3(byte[] signature) =>
        signature.AsSpan().StartsWith("ID3"u8) ||
        signature.Length >= 2 && signature[0] == 0xFF && (signature[1] & 0xE0) == 0xE0;

    private static bool IsFlac(byte[] signature) =>
        signature.AsSpan().StartsWith("fLaC"u8);

    private static bool IsMp4(byte[] signature) =>
        signature.Length >= 12 && signature.AsSpan(4, 4).SequenceEqual("ftyp"u8);

    private static bool IsAac(byte[] signature) =>
        signature.AsSpan().StartsWith("ADIF"u8) ||
        signature.Length >= 2 && signature[0] == 0xFF && (signature[1] & 0xF6) == 0xF0;

    private static string DetectCodec(byte[] signature, string filePath)
    {
        if (IsOggVorbis(signature))
        {
            return "Ogg/Vorbis";
        }

        if (IsWave(signature))
        {
            return "WAV";
        }

        if (IsFlac(signature))
        {
            return "FLAC";
        }

        if (IsMp4(signature))
        {
            return "MPEG-4";
        }

        if (IsAac(signature))
        {
            return "AAC";
        }

        if (IsMp3(signature))
        {
            return "MP3";
        }

        return Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
    }
}

public sealed class AudioFileMetadataReader : IAudioFileMetadataReader
{
    public Task<AudioFileMetadata> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        var normalizedPath = PathNormalizer.NormalizeFilePath(filePath);
        var extension = Path.GetExtension(normalizedPath).ToLowerInvariant();
        if (!Sound.AllowedExtensions.Contains(extension))
        {
            throw new AudioFileMetadataException(normalizedPath, "The audio format is not supported.");
        }

        try
        {
            var decoded = AudioFileDecoder.Decode(normalizedPath, cancellationToken);
            var info = new FileInfo(normalizedPath);
            return Task.FromResult(new AudioFileMetadata(
                Path.GetFileNameWithoutExtension(normalizedPath),
                normalizedPath,
                extension,
                decoded.Duration,
                info.Length,
                ExtractWaveform(decoded.Samples, 32)));
        }
        catch (FileNotFoundException exception)
        {
            throw new AudioFileUnreadableException(normalizedPath, exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new AudioFileUnreadableException(normalizedPath, exception.Message);
        }
        catch (InvalidDataException exception)
        {
            throw new AudioFileMetadataException(normalizedPath, exception.Message);
        }
    }

    private static byte[] ExtractWaveform(float[] samples, int peakCount)
    {
        var samplesPerPeak = Math.Max(1, samples.Length / peakCount);
        var peaks = new byte[peakCount];
        for (var peak = 0; peak < peakCount; peak++)
        {
            var start = peak * samplesPerPeak;
            var end = peak == peakCount - 1 ? samples.Length : Math.Min(samples.Length, start + samplesPerPeak);
            var maximum = 0f;
            for (var index = start; index < end; index++)
            {
                maximum = Math.Max(maximum, Math.Abs(samples[index]));
            }

            peaks[peak] = (byte)Math.Clamp((int)Math.Round(maximum * byte.MaxValue), 0, byte.MaxValue);
        }

        return peaks;
    }
}

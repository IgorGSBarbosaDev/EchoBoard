using EchoBoard.Application.Audio;
using EchoBoard.Audio.Microphone;
using FluentAssertions;
using Xunit;

namespace EchoBoard.Audio.Tests;

public sealed class MicrophonePcmRingBufferTests
{
    [Fact]
    public void OverflowDropsOldestSamplesAndKeepsBufferedDurationBounded()
    {
        var buffer = new MicrophonePcmRingBuffer(
            new AudioStreamFormatDto(1000, 1, 32, "IeeeFloat"),
            capacitySamples: 4);

        buffer.WriteProcessed([0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f], gain: 1, isMuted: false);

        buffer.BufferedSamples.Should().Be(4);
        buffer.BufferedDuration.Should().Be(TimeSpan.FromMilliseconds(4));
        buffer.DroppedSamples.Should().Be(2);

        var samples = new float[4];
        buffer.TryRead(samples, out var samplesWritten).Should().BeTrue();
        samplesWritten.Should().Be(4);
        samples.Should().Equal(0.3f, 0.4f, 0.5f, 0.6f);
        buffer.BufferedDuration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ControllerAllocatesOnlyTheTargetMicrophoneLatencyBuffer()
    {
        var format = new AudioStreamFormatDto(48000, 1, 32, "IeeeFloat");
        var buffer = new MicrophonePcmRingBuffer(
            format,
            AudioLatencyConfiguration.CalculateBufferCapacitySamples(format.SampleRate, format.Channels));

        buffer.CapacitySamples.Should().Be(960);
        buffer.BufferedDuration.Should().Be(TimeSpan.Zero);
    }
}

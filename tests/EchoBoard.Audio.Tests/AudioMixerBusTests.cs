using EchoBoard.Audio.Mixing;
using FluentAssertions;
using NAudio.Wave;
using Xunit;

namespace EchoBoard.Audio.Tests;

public sealed class AudioMixerBusTests
{
    private static readonly WaveFormat Format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    [Fact]
    public void VirtualBusMixesVoiceAndEffectsWhileMonitorContainsOnlyEffects()
    {
        var virtualBus = new AudioMixerBus(Format, () => 1);
        var monitorBus = new AudioMixerBus(Format, () => 1);
        virtualBus.AddInput(new ConstantSampleProvider(0.25f));
        virtualBus.AddInput(new ConstantSampleProvider(0.5f));
        monitorBus.AddInput(new ConstantSampleProvider(0.5f));
        var virtualSamples = new float[16];
        var monitorSamples = new float[16];

        virtualBus.Read(virtualSamples, 0, virtualSamples.Length);
        monitorBus.Read(monitorSamples, 0, monitorSamples.Length);

        virtualSamples.Should().OnlyContain(sample => Math.Abs(sample - 0.75f) < 0.0001f);
        monitorSamples.Should().OnlyContain(sample => Math.Abs(sample - 0.5f) < 0.0001f);
    }

    [Fact]
    public void RouteGainSupportsVolumeMuteAndLimitingWithoutChangingInputs()
    {
        var gain = 0.5;
        var bus = new AudioMixerBus(Format, () => gain);
        bus.AddInput(new ConstantSampleProvider(3f));
        var samples = new float[8];

        bus.Read(samples, 0, samples.Length);
        samples.Should().OnlyContain(sample => sample == 1f);

        gain = 0;
        bus.Read(samples, 0, samples.Length);
        samples.Should().OnlyContain(sample => sample == 0f);
    }

    [Fact]
    public void VoiceContinuesBeforeDuringAndAfterFiniteEffect()
    {
        var bus = new AudioMixerBus(Format, () => 1);
        bus.AddInput(new ConstantSampleProvider(0.2f));
        var before = new float[4];
        bus.Read(before, 0, before.Length);
        bus.AddInput(new FiniteSampleProvider([0.3f, 0.3f, 0.3f, 0.3f]));
        var during = new float[4];
        var after = new float[4];

        bus.Read(during, 0, during.Length);
        bus.Read(after, 0, after.Length);

        before.Should().OnlyContain(sample => Math.Abs(sample - 0.2f) < 0.0001f);
        during.Should().OnlyContain(sample => Math.Abs(sample - 0.5f) < 0.0001f);
        after.Should().OnlyContain(sample => Math.Abs(sample - 0.2f) < 0.0001f);
    }

    private sealed class ConstantSampleProvider(float value) : ISampleProvider
    {
        public WaveFormat WaveFormat => Format;

        public int Read(float[] buffer, int offset, int count)
        {
            buffer.AsSpan(offset, count).Fill(value);
            return count;
        }
    }

    private sealed class FiniteSampleProvider(float[] samples) : ISampleProvider
    {
        private int position;

        public WaveFormat WaveFormat => Format;

        public int Read(float[] buffer, int offset, int count)
        {
            var read = Math.Min(count, samples.Length - position);
            samples.AsSpan(position, read).CopyTo(buffer.AsSpan(offset, read));
            position += read;
            return read;
        }
    }
}

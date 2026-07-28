using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace EchoBoard.Audio.Mixing;

internal sealed class AudioMixerBus : ISampleProvider
{
    private readonly MixingSampleProvider mixer;
    private readonly Func<double> gain;
    private readonly AudioPeakMeter peakMeter = new();

    public AudioMixerBus(WaveFormat waveFormat, Func<double> gain)
    {
        mixer = new MixingSampleProvider(waveFormat) { ReadFully = true };
        this.gain = gain;
    }

    public WaveFormat WaveFormat => mixer.WaveFormat;

    public double ConsumePeakLevel() => peakMeter.Consume();

    public long PeakGeneration => peakMeter.Generation;

    public void AddInput(ISampleProvider input) => mixer.AddMixerInput(input);

    public void RemoveInput(ISampleProvider input) => mixer.RemoveMixerInput(input);

    public int Read(float[] buffer, int offset, int count)
    {
        var read = mixer.Read(buffer, offset, count);
        var currentGain = (float)Math.Clamp(gain(), 0.0, 1.0);
        for (var index = offset; index < offset + read; index++)
        {
            buffer[index] = Math.Clamp(buffer[index] * currentGain, -1f, 1f);
        }

        peakMeter.Report(buffer.AsSpan(offset, read));

        return read;
    }
}

internal sealed class AudioPeakMeter
{
    private int peakBits;
    private long generation;

    public long Generation => Volatile.Read(ref generation);

    public void Report(ReadOnlySpan<float> samples)
    {
        var peak = 0f;
        foreach (var sample in samples)
        {
            peak = Math.Max(peak, Math.Abs(sample));
        }

        Report(peak);
    }

    public void Report(float peak)
    {
        peak = Math.Clamp(float.IsFinite(peak) ? peak : 0f, 0f, 1f);
        Interlocked.Increment(ref generation);
        var previousBits = Volatile.Read(ref peakBits);
        while (true)
        {
            var previous = BitConverter.Int32BitsToSingle(previousBits);
            if (previous >= peak)
            {
                return;
            }

            var nextBits = BitConverter.SingleToInt32Bits(peak);
            var observedBits = Interlocked.CompareExchange(ref peakBits, nextBits, previousBits);
            if (observedBits == previousBits)
            {
                return;
            }

            previousBits = observedBits;
        }
    }

    public double Consume()
    {
        return Math.Clamp(
            BitConverter.Int32BitsToSingle(Interlocked.Exchange(ref peakBits, 0)),
            0f,
            1f);
    }
}

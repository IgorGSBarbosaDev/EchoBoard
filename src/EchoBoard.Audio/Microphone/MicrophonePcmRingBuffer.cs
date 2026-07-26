using EchoBoard.Application.Audio;

namespace EchoBoard.Audio.Microphone;

public sealed class MicrophonePcmRingBuffer : IMicrophonePcmSource
{
    private readonly float[] buffer;
    private long readSequence;
    private long writeSequence;

    public MicrophonePcmRingBuffer(AudioStreamFormatDto format, int capacitySamples)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacitySamples);

        Format = format;
        buffer = new float[capacitySamples];
    }

    public AudioStreamFormatDto Format { get; }

    public int CapacitySamples => buffer.Length;

    public bool TryRead(Span<float> destination, out int samplesWritten)
    {
        var read = Volatile.Read(ref readSequence);
        var write = Volatile.Read(ref writeSequence);
        samplesWritten = (int)Math.Min(destination.Length, Math.Max(0, write - read));
        if (samplesWritten == 0)
        {
            return false;
        }

        for (var i = 0; i < samplesWritten; i++)
        {
            destination[i] = buffer[(read + i) % buffer.Length];
        }

        AdvanceReadToAtLeast(read + samplesWritten);
        return true;
    }

    public double WriteProcessed(ReadOnlySpan<float> samples, double gain, bool isMuted)
    {
        var level = 0.0;
        var write = Volatile.Read(ref writeSequence);
        for (var i = 0; i < samples.Length; i++, write++)
        {
            var sample = isMuted ? 0f : (float)Math.Clamp(samples[i] * gain, -1.0, 1.0);
            level = Math.Max(level, Math.Abs(sample));

            buffer[write % buffer.Length] = sample;
        }

        Volatile.Write(ref writeSequence, write);
        AdvanceReadToAtLeast(Math.Max(0, write - buffer.Length));
        return Math.Clamp(level, 0.0, 1.0);
    }

    public void Clear()
    {
        Volatile.Write(ref readSequence, 0);
        Volatile.Write(ref writeSequence, 0);
        Array.Clear(buffer);
    }

    private void AdvanceReadToAtLeast(long target)
    {
        while (true)
        {
            var current = Volatile.Read(ref readSequence);
            if (current >= target)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref readSequence, target, current) == current)
            {
                return;
            }
        }
    }
}

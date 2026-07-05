using EchoBoard.Application.Audio;

namespace EchoBoard.Audio.Microphone;

public sealed class MicrophonePcmRingBuffer : IMicrophonePcmSource
{
    private readonly float[] buffer;
    private int readIndex;
    private int writeIndex;
    private int availableSamples;

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
        samplesWritten = Math.Min(destination.Length, availableSamples);
        if (samplesWritten == 0)
        {
            return false;
        }

        for (var i = 0; i < samplesWritten; i++)
        {
            destination[i] = buffer[readIndex];
            readIndex = (readIndex + 1) % buffer.Length;
        }

        availableSamples -= samplesWritten;
        return true;
    }

    public double WriteProcessed(ReadOnlySpan<float> samples, double gain, bool isMuted)
    {
        var level = 0.0;
        for (var i = 0; i < samples.Length; i++)
        {
            var sample = isMuted ? 0f : (float)Math.Clamp(samples[i] * gain, -1.0, 1.0);
            level = Math.Max(level, Math.Abs(sample));

            buffer[writeIndex] = sample;
            writeIndex = (writeIndex + 1) % buffer.Length;
            if (availableSamples == buffer.Length)
            {
                readIndex = (readIndex + 1) % buffer.Length;
            }
            else
            {
                availableSamples++;
            }
        }

        return Math.Clamp(level, 0.0, 1.0);
    }

    public void Clear()
    {
        readIndex = 0;
        writeIndex = 0;
        availableSamples = 0;
        Array.Clear(buffer);
    }
}

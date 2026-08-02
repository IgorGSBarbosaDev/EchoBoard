namespace EchoBoard.Audio;

internal static class AudioLatencyConfiguration
{
    public const int TargetBufferMilliseconds = 20;

    public static int CalculateBufferCapacitySamples(int sampleRate, int channels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channels);

        var samples = (double)sampleRate * channels * (TargetBufferMilliseconds / 1000.0);
        return Math.Max(1, checked((int)Math.Ceiling(samples)));
    }
}

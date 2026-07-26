using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace EchoBoard.Audio.Mixing;

internal sealed class AudioMixerBus : ISampleProvider
{
    private readonly MixingSampleProvider mixer;
    private readonly Func<double> gain;

    public AudioMixerBus(WaveFormat waveFormat, Func<double> gain)
    {
        mixer = new MixingSampleProvider(waveFormat) { ReadFully = true };
        this.gain = gain;
    }

    public WaveFormat WaveFormat => mixer.WaveFormat;

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

        return read;
    }
}

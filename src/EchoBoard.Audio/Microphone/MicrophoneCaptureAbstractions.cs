using EchoBoard.Application.Audio;

namespace EchoBoard.Audio.Microphone;

public interface IAudioInputDeviceEnumerator
{
    Task<IReadOnlyList<AudioInputDeviceDto>> ListAsync(CancellationToken cancellationToken);
}

public interface IMicrophoneCaptureSessionFactory
{
    IMicrophoneCaptureSession Create(AudioInputDeviceDto device);
}

public interface IMicrophoneCaptureSession : IAsyncDisposable
{
    event EventHandler<MicrophoneSamplesCapturedEventArgs>? SamplesCaptured;

    event EventHandler<Exception>? CaptureFailed;

    AudioStreamFormatDto Format { get; }

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

public sealed class MicrophoneSamplesCapturedEventArgs : EventArgs
{
    public MicrophoneSamplesCapturedEventArgs(float[] samples)
    {
        Samples = samples;
    }

    public float[] Samples { get; }
}

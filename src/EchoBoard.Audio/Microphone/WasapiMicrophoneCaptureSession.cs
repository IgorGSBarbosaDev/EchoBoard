using EchoBoard.Application.Audio;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace EchoBoard.Audio.Microphone;

public sealed class WasapiMicrophoneCaptureSession : IMicrophoneCaptureSession
{
    private readonly WasapiCapture capture;

    public WasapiMicrophoneCaptureSession(MMDevice device)
    {
        capture = new WasapiCapture(device);
        capture.DataAvailable += OnDataAvailable;
        capture.RecordingStopped += OnRecordingStopped;
        Format = ToFormat(capture.WaveFormat);
    }

    public event EventHandler<MicrophoneSamplesCapturedEventArgs>? SamplesCaptured;

    public event EventHandler<Exception>? CaptureFailed;

    public AudioStreamFormatDto Format { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        capture.StartRecording();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        capture.StopRecording();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        capture.DataAvailable -= OnDataAvailable;
        capture.RecordingStopped -= OnRecordingStopped;
        capture.Dispose();
        return ValueTask.CompletedTask;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            var samples = ConvertToFloatSamples(capture.WaveFormat, e.Buffer, e.BytesRecorded);
            SamplesCaptured?.Invoke(this, new MicrophoneSamplesCapturedEventArgs(samples));
        }
        catch (Exception exception)
        {
            CaptureFailed?.Invoke(this, exception);
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
        {
            CaptureFailed?.Invoke(this, e.Exception);
        }
    }

    private static AudioStreamFormatDto ToFormat(WaveFormat format)
    {
        return new AudioStreamFormatDto(
            format.SampleRate,
            format.Channels,
            format.BitsPerSample,
            format.Encoding.ToString());
    }

    private static float[] ConvertToFloatSamples(WaveFormat format, byte[] buffer, int bytesRecorded)
    {
        if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
        {
            var sampleCount = bytesRecorded / sizeof(float);
            var samples = new float[sampleCount];
            Buffer.BlockCopy(buffer, 0, samples, 0, sampleCount * sizeof(float));
            return samples;
        }

        if (format.BitsPerSample == 16)
        {
            var sampleCount = bytesRecorded / sizeof(short);
            var samples = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var value = BitConverter.ToInt16(buffer, i * sizeof(short));
                samples[i] = value / 32768f;
            }

            return samples;
        }

        throw new InvalidOperationException($"Unsupported microphone format: {format.Encoding} {format.BitsPerSample}-bit.");
    }
}

using System.Buffers;
using EchoBoard.Application.Audio;
using EchoBoard.Audio;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace EchoBoard.Audio.Microphone;

public sealed class WasapiMicrophoneCaptureSession : IMicrophoneCaptureSession
{
    private readonly WasapiCapture capture;

    public WasapiMicrophoneCaptureSession(MMDevice device)
    {
        capture = new WasapiCapture(
            device,
            useEventSync: true,
            audioBufferMillisecondsLength: AudioLatencyConfiguration.TargetBufferMilliseconds);
        capture.DataAvailable += OnDataAvailable;
        capture.RecordingStopped += OnRecordingStopped;
        Format = ToFormat(capture.WaveFormat);
    }

    public event MicrophoneSamplesCapturedHandler? SamplesCaptured;

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
        float[]? sampleBuffer = null;
        try
        {
            var sampleCount = GetSampleCount(capture.WaveFormat, e.BytesRecorded);
            if (sampleCount == 0)
            {
                return;
            }

            sampleBuffer = ArrayPool<float>.Shared.Rent(sampleCount);
            ConvertToFloatSamples(capture.WaveFormat, e.Buffer, sampleBuffer, sampleCount);
            SamplesCaptured?.Invoke(this, sampleBuffer.AsSpan(0, sampleCount));
        }
        catch (Exception exception)
        {
            CaptureFailed?.Invoke(this, exception);
        }
        finally
        {
            if (sampleBuffer is not null)
            {
                ArrayPool<float>.Shared.Return(sampleBuffer);
            }
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

    private static int GetSampleCount(WaveFormat format, int bytesRecorded)
    {
        if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
        {
            return bytesRecorded / sizeof(float);
        }

        if (format.BitsPerSample == 16)
        {
            return bytesRecorded / sizeof(short);
        }

        throw new InvalidOperationException($"Unsupported microphone format: {format.Encoding} {format.BitsPerSample}-bit.");
    }

    private static void ConvertToFloatSamples(
        WaveFormat format,
        byte[] buffer,
        float[] destination,
        int sampleCount)
    {
        if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
        {
            Buffer.BlockCopy(buffer, 0, destination, 0, sampleCount * sizeof(float));
            return;
        }

        if (format.BitsPerSample == 16)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                var value = BitConverter.ToInt16(buffer, i * sizeof(short));
                destination[i] = value / 32768f;
            }

            return;
        }

        throw new InvalidOperationException($"Unsupported microphone format: {format.Encoding} {format.BitsPerSample}-bit.");
    }
}

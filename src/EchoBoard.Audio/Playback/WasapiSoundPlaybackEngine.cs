using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using EchoBoard.Application.Audio;
using NAudio.CoreAudioApi;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace EchoBoard.Audio.Playback;

public sealed class WasapiSoundPlaybackEngine : ISoundPlaybackEngine, IDisposable
{
    private readonly ConcurrentDictionary<Guid, PlaybackSession> sessions = new();
    private Guid? latestSessionId;
    private bool disposed;

    public Task PlayAsync(string filePath, double volume, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(disposed, this);

        var reader = OpenAudioReader(filePath);
        var volumeProvider = new VolumeSampleProvider(reader.ToSampleProvider())
        {
            Volume = (float)Math.Clamp(volume, 0.0, 1.0)
        };

        WasapiOut? output = null;
        PlaybackSession? session = null;
        Guid? sessionId = null;
        try
        {
            output = new WasapiOut(AudioClientShareMode.Shared, useEventSync: false, latency: 50);
            output.Init(volumeProvider);

            var id = Guid.NewGuid();
            session = new PlaybackSession(output, reader, volumeProvider, filePath, () => RemoveSession(id));
            if (!sessions.TryAdd(id, session))
            {
                session.Dispose();
                throw new InvalidOperationException("Audio playback could not be started.");
            }

            sessionId = id;
            latestSessionId = id;
            output.Play();
            return Task.CompletedTask;
        }
        catch
        {
            if (session is not null)
            {
                if (sessionId is not null)
                {
                    sessions.TryRemove(sessionId.Value, out _);
                }

                session.Dispose();
            }
            else
            {
                output?.Dispose();
                reader.Dispose();
            }

            throw;
        }
    }

    public Task StopAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var session in sessions.Values)
        {
            session.Stop();
        }

        return Task.CompletedTask;
    }

    public Task TogglePauseAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var activeSessions = sessions.Values.ToArray();
        var shouldPause = activeSessions.Any(session => session.PlaybackState == PlaybackState.Playing);
        foreach (var session in activeSessions)
        {
            if (shouldPause)
            {
                session.Pause();
            }
            else
            {
                session.Resume();
            }
        }

        return Task.CompletedTask;
    }

    public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var session in sessions.Values)
        {
            session.Seek(position);
        }

        return Task.CompletedTask;
    }

    public Task SetVolumeAsync(double volume, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var session in sessions.Values)
        {
            session.SetVolume(volume);
        }

        return Task.CompletedTask;
    }

    public SoundPlaybackSnapshot GetSnapshot()
    {
        PlaybackSession? session = null;
        if (latestSessionId is Guid id)
        {
            sessions.TryGetValue(id, out session);
        }

        session ??= sessions.Values.LastOrDefault();
        if (session is null)
        {
            return SoundPlaybackSnapshot.Idle;
        }

        return session.GetSnapshot();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (var session in sessions.Values)
        {
            session.Dispose();
        }

        sessions.Clear();
    }

    private static WaveStream OpenAudioReader(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (string.Equals(extension, ".ogg", StringComparison.OrdinalIgnoreCase))
        {
            return new VorbisWaveReader(filePath);
        }

        if (string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return new AudioFileReader(filePath);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or COMException or NotSupportedException or ArgumentException)
            {
                return new Mp3FileReader(filePath);
            }
        }

        return new AudioFileReader(filePath);
    }

    private void RemoveSession(Guid id)
    {
        if (sessions.TryRemove(id, out var session))
        {
            session.Dispose();
        }
    }

    private sealed class PlaybackSession : IDisposable
    {
        private readonly object sync = new();
        private readonly IWavePlayer output;
        private readonly WaveStream reader;
        private readonly VolumeSampleProvider volumeProvider;
        private readonly string filePath;
        private readonly Action completed;
        private int disposed;

        public PlaybackSession(IWavePlayer output, WaveStream reader, VolumeSampleProvider volumeProvider, string filePath, Action completed)
        {
            this.output = output;
            this.reader = reader;
            this.volumeProvider = volumeProvider;
            this.filePath = filePath;
            this.completed = completed;
            this.output.PlaybackStopped += OnPlaybackStopped;
        }

        public PlaybackState PlaybackState => output.PlaybackState;

        public void Stop() => output.Stop();

        public void Pause() => output.Pause();

        public void Resume() => output.Play();

        public void Seek(TimeSpan position)
        {
            lock (sync)
            {
                reader.CurrentTime = position < TimeSpan.Zero
                    ? TimeSpan.Zero
                    : position > reader.TotalTime ? reader.TotalTime : position;
            }
        }

        public void SetVolume(double volume) => volumeProvider.Volume = (float)Math.Clamp(volume, 0.0, 1.0);

        public SoundPlaybackSnapshot GetSnapshot()
        {
            lock (sync)
            {
                if (disposed != 0)
                {
                    return SoundPlaybackSnapshot.Idle;
                }

                return new(
                    filePath,
                    reader.CurrentTime,
                    reader.TotalTime,
                    output.PlaybackState == PlaybackState.Playing,
                    output.PlaybackState == PlaybackState.Paused);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            lock (sync)
            {
                output.PlaybackStopped -= OnPlaybackStopped;
                output.Dispose();
                reader.Dispose();
            }
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e) => completed();
    }
}

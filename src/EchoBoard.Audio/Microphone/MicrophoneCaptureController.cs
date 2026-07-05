using EchoBoard.Application.Audio;

namespace EchoBoard.Audio.Microphone;

public sealed class MicrophoneCaptureController : IMicrophoneCaptureController, IAsyncDisposable
{
    private readonly IAudioInputDeviceEnumerator devices;
    private readonly IMicrophoneCaptureSessionFactory sessionFactory;
    private IMicrophoneCaptureSession? session;
    private MicrophonePcmRingBuffer? source;
    private MicrophoneCaptureSnapshot snapshot = MicrophoneCaptureSnapshot.Stopped();

    public MicrophoneCaptureController(
        IAudioInputDeviceEnumerator devices,
        IMicrophoneCaptureSessionFactory sessionFactory)
    {
        this.devices = devices;
        this.sessionFactory = sessionFactory;
    }

    public IMicrophonePcmSource? CurrentSource => source;

    public Task<IReadOnlyList<AudioInputDeviceDto>> ListInputDevicesAsync(CancellationToken cancellationToken)
    {
        return devices.ListAsync(cancellationToken);
    }

    public async Task RestoreSelectionAsync(MicrophoneSettingsDto settings, CancellationToken cancellationToken)
    {
        MicrophoneCaptureSnapshot.ValidateGain(settings.Gain);
        var availableDevices = await devices.ListAsync(cancellationToken);
        if (availableDevices.Count == 0)
        {
            snapshot = MicrophoneCaptureSnapshot.Unavailable("No microphone available. Connect an input device.", settings);
            return;
        }

        var selected = availableDevices.SingleOrDefault(item => string.Equals(item.Id, settings.SelectedDeviceId, StringComparison.Ordinal));
        if (selected is null && !string.IsNullOrWhiteSpace(settings.SelectedDeviceId))
        {
            snapshot = MicrophoneCaptureSnapshot.Unavailable($"Previous microphone unavailable: {settings.SelectedDeviceName ?? settings.SelectedDeviceId}", settings);
            return;
        }

        selected ??= availableDevices.FirstOrDefault(item => item.IsDefault) ?? availableDevices[0];
        var restored = settings with
        {
            SelectedDeviceId = selected.Id,
            SelectedDeviceName = selected.Name
        };
        snapshot = MicrophoneCaptureSnapshot.Stopped(selected, restored);
    }

    public async Task SelectDeviceAsync(AudioInputDeviceDto device, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(device);
        await StopAsync(cancellationToken);
        snapshot = MicrophoneCaptureSnapshot.Stopped(device, snapshot.Settings with
        {
            SelectedDeviceId = device.Id,
            SelectedDeviceName = device.Name
        });
    }

    public Task SetGainAsync(double gain, CancellationToken cancellationToken)
    {
        MicrophoneCaptureSnapshot.ValidateGain(gain);
        snapshot = snapshot with { Gain = gain };
        return Task.CompletedTask;
    }

    public Task SetMutedAsync(bool isMuted, CancellationToken cancellationToken)
    {
        snapshot = snapshot with
        {
            IsMuted = isMuted,
            Level = isMuted ? 0 : snapshot.Level,
            StatusMessage = isMuted ? "Muted" : StatusFor(snapshot.State)
        };
        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(snapshot.SelectedDeviceId) || string.IsNullOrWhiteSpace(snapshot.SelectedDeviceName))
        {
            snapshot = snapshot with
            {
                State = MicrophoneCaptureState.Unavailable,
                Level = 0,
                StatusMessage = "Select a microphone before starting capture."
            };
            return;
        }

        await StopSessionAsync(cancellationToken);
        var selectedDevice = new AudioInputDeviceDto(snapshot.SelectedDeviceId, snapshot.SelectedDeviceName, IsDefault: false, IsAvailable: true);
        snapshot = snapshot with
        {
            State = MicrophoneCaptureState.Starting,
            Level = 0,
            StatusMessage = "Starting",
            ErrorMessage = null,
            Format = null
        };

        try
        {
            session = sessionFactory.Create(selectedDevice);
            source = new MicrophonePcmRingBuffer(
                session.Format,
                Math.Max(session.Format.SampleRate * Math.Max(session.Format.Channels, 1) * 2, 4096));
            session.SamplesCaptured += OnSamplesCaptured;
            session.CaptureFailed += OnCaptureFailed;
            await session.StartAsync(cancellationToken);
            snapshot = snapshot with
            {
                State = MicrophoneCaptureState.Active,
                StatusMessage = snapshot.IsMuted ? "Muted" : "Capturing",
                Format = session.Format
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            DisposeSessionSynchronously();
            snapshot = snapshot with
            {
                State = MicrophoneCaptureState.Failed,
                Level = 0,
                StatusMessage = "Microphone capture failed to start.",
                ErrorMessage = exception.Message
            };
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopSessionAsync(cancellationToken);
        source?.Clear();
        source = null;
        snapshot = snapshot with
        {
            State = MicrophoneCaptureState.Stopped,
            Level = 0,
            StatusMessage = "Stopped",
            ErrorMessage = null,
            Format = null
        };
    }

    public MicrophoneCaptureSnapshot GetSnapshot()
    {
        return snapshot;
    }

    public async ValueTask DisposeAsync()
    {
        await StopSessionAsync(CancellationToken.None);
    }

    private void OnSamplesCaptured(object? sender, MicrophoneSamplesCapturedEventArgs e)
    {
        var currentSource = source;
        if (currentSource is null || snapshot.State != MicrophoneCaptureState.Active)
        {
            return;
        }

        var level = currentSource.WriteProcessed(e.Samples, snapshot.Gain, snapshot.IsMuted);
        snapshot = snapshot with
        {
            Level = snapshot.IsMuted ? 0 : level,
            StatusMessage = snapshot.IsMuted ? "Muted" : "Capturing"
        };
    }

    private void OnCaptureFailed(object? sender, Exception exception)
    {
        DisposeSessionSynchronously();
        source?.Clear();
        source = null;
        snapshot = snapshot with
        {
            State = MicrophoneCaptureState.Unavailable,
            Level = 0,
            StatusMessage = "Microphone unavailable. Reconnect it or choose another input.",
            ErrorMessage = exception.Message,
            Format = null
        };
    }

    private async Task StopSessionAsync(CancellationToken cancellationToken)
    {
        var currentSession = session;
        session = null;
        if (currentSession is null)
        {
            return;
        }

        currentSession.SamplesCaptured -= OnSamplesCaptured;
        currentSession.CaptureFailed -= OnCaptureFailed;
        await currentSession.StopAsync(cancellationToken);
        await currentSession.DisposeAsync();
    }

    private void DisposeSessionSynchronously()
    {
        var currentSession = session;
        session = null;
        if (currentSession is null)
        {
            return;
        }

        currentSession.SamplesCaptured -= OnSamplesCaptured;
        currentSession.CaptureFailed -= OnCaptureFailed;
        currentSession.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static string StatusFor(MicrophoneCaptureState state)
    {
        return state switch
        {
            MicrophoneCaptureState.Active => "Capturing",
            MicrophoneCaptureState.Starting => "Starting",
            MicrophoneCaptureState.Unavailable => "Unavailable",
            MicrophoneCaptureState.Failed => "Failed",
            _ => "Stopped"
        };
    }
}

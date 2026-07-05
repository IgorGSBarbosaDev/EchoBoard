using EchoBoard.Application.Audio;
using EchoBoard.Audio.Microphone;
using FluentAssertions;
using Xunit;

namespace EchoBoard.Audio.Tests;

public sealed class MicrophoneCaptureControllerTests
{
    [Fact]
    public async Task ControllerCapturesLifecycleWithFakeWasapiSession()
    {
        var session = new FakeMicrophoneCaptureSession(new AudioStreamFormatDto(48000, 1, 32, "IeeeFloat"));
        var factory = new FakeMicrophoneCaptureSessionFactory(session);
        var devices = new FakeAudioInputDeviceEnumerator([new AudioInputDeviceDto("mic-1", "Desk Mic", true, true)]);
        var controller = new MicrophoneCaptureController(devices, factory);
        await controller.RestoreSelectionAsync(new MicrophoneSettingsDto("mic-1", "Desk Mic", 1.0, false), CancellationToken.None);

        await controller.StartAsync(CancellationToken.None);
        session.PushSamples([0.25f, -0.5f, 0.75f]);

        var active = controller.GetSnapshot();
        active.State.Should().Be(MicrophoneCaptureState.Active);
        active.Level.Should().BeApproximately(0.75, 0.0001);
        controller.CurrentSource.Should().NotBeNull();

        await controller.StopAsync(CancellationToken.None);

        controller.GetSnapshot().State.Should().Be(MicrophoneCaptureState.Stopped);
        controller.GetSnapshot().Level.Should().Be(0);
        session.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task MutedCaptureReportsZeroLevelAndStillBuffersSilence()
    {
        var session = new FakeMicrophoneCaptureSession(new AudioStreamFormatDto(48000, 1, 32, "IeeeFloat"));
        var controller = CreateStartedController(session);
        await controller.SetMutedAsync(true, CancellationToken.None);

        session.PushSamples([0.8f]);

        controller.GetSnapshot().Level.Should().Be(0);
        controller.GetSnapshot().StatusMessage.Should().Be("Muted");
    }

    [Fact]
    public async Task GainScalesLevelAndPcmSourceSamples()
    {
        var session = new FakeMicrophoneCaptureSession(new AudioStreamFormatDto(48000, 1, 32, "IeeeFloat"));
        var controller = CreateStartedController(session);
        await controller.SetGainAsync(0.5, CancellationToken.None);

        session.PushSamples([0.8f, -0.4f]);
        var buffer = new float[2];
        var read = controller.CurrentSource!.TryRead(buffer, out var samplesWritten);

        read.Should().BeTrue();
        samplesWritten.Should().Be(2);
        buffer.Should().Equal(0.4f, -0.2f);
        controller.GetSnapshot().Level.Should().BeApproximately(0.4, 0.0001);
    }

    [Fact]
    public async Task StartFailureTransitionsToFailedState()
    {
        var factory = new FakeMicrophoneCaptureSessionFactory(new InvalidOperationException("access denied"));
        var devices = new FakeAudioInputDeviceEnumerator([new AudioInputDeviceDto("mic-1", "Desk Mic", true, true)]);
        var controller = new MicrophoneCaptureController(devices, factory);
        await controller.RestoreSelectionAsync(new MicrophoneSettingsDto("mic-1", "Desk Mic", 1.0, false), CancellationToken.None);

        await controller.StartAsync(CancellationToken.None);

        controller.GetSnapshot().State.Should().Be(MicrophoneCaptureState.Failed);
        controller.GetSnapshot().ErrorMessage.Should().Contain("access denied");
    }

    [Fact]
    public async Task RuntimeFailureTransitionsToUnavailableAndDisposesSession()
    {
        var session = new FakeMicrophoneCaptureSession(new AudioStreamFormatDto(48000, 1, 32, "IeeeFloat"));
        var controller = CreateStartedController(session);

        session.Fail(new InvalidOperationException("device removed"));

        controller.GetSnapshot().State.Should().Be(MicrophoneCaptureState.Unavailable);
        controller.GetSnapshot().Level.Should().Be(0);
        session.IsDisposed.Should().BeTrue();
    }

    private static MicrophoneCaptureController CreateStartedController(FakeMicrophoneCaptureSession session)
    {
        var factory = new FakeMicrophoneCaptureSessionFactory(session);
        var devices = new FakeAudioInputDeviceEnumerator([new AudioInputDeviceDto("mic-1", "Desk Mic", true, true)]);
        var controller = new MicrophoneCaptureController(devices, factory);
        controller.RestoreSelectionAsync(new MicrophoneSettingsDto("mic-1", "Desk Mic", 1.0, false), CancellationToken.None).GetAwaiter().GetResult();
        controller.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        return controller;
    }

    private sealed class FakeAudioInputDeviceEnumerator : IAudioInputDeviceEnumerator
    {
        private readonly IReadOnlyList<AudioInputDeviceDto> devices;

        public FakeAudioInputDeviceEnumerator(IReadOnlyList<AudioInputDeviceDto> devices)
        {
            this.devices = devices;
        }

        public Task<IReadOnlyList<AudioInputDeviceDto>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(devices);
        }
    }

    private sealed class FakeMicrophoneCaptureSessionFactory : IMicrophoneCaptureSessionFactory
    {
        private readonly FakeMicrophoneCaptureSession? session;
        private readonly Exception? exception;

        public FakeMicrophoneCaptureSessionFactory(FakeMicrophoneCaptureSession session)
        {
            this.session = session;
        }

        public FakeMicrophoneCaptureSessionFactory(Exception exception)
        {
            this.exception = exception;
        }

        public IMicrophoneCaptureSession Create(AudioInputDeviceDto device)
        {
            if (exception is not null)
            {
                throw exception;
            }

            return session!;
        }
    }

    private sealed class FakeMicrophoneCaptureSession : IMicrophoneCaptureSession
    {
        public FakeMicrophoneCaptureSession(AudioStreamFormatDto format)
        {
            Format = format;
        }

        public event EventHandler<MicrophoneSamplesCapturedEventArgs>? SamplesCaptured;

        public event EventHandler<Exception>? CaptureFailed;

        public AudioStreamFormatDto Format { get; }

        public bool IsDisposed { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void PushSamples(float[] samples)
        {
            SamplesCaptured?.Invoke(this, new MicrophoneSamplesCapturedEventArgs(samples));
        }

        public void Fail(Exception exception)
        {
            CaptureFailed?.Invoke(this, exception);
        }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}

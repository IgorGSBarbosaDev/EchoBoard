using EchoBoard.Application.Audio;
using NAudio.CoreAudioApi;

namespace EchoBoard.Audio.Microphone;

public sealed class WasapiMicrophoneCaptureSessionFactory : IMicrophoneCaptureSessionFactory
{
    public IMicrophoneCaptureSession Create(AudioInputDeviceDto device)
    {
        using var enumerator = new MMDeviceEnumerator();
        var endpoint = enumerator.GetDevice(device.Id);
        return new WasapiMicrophoneCaptureSession(endpoint);
    }
}

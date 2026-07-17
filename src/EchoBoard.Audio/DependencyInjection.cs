using Microsoft.Extensions.DependencyInjection;
using EchoBoard.Application.Audio;
using EchoBoard.Audio.Microphone;
using EchoBoard.Audio.Playback;

namespace EchoBoard.Audio;

public static class DependencyInjection
{
    public static IServiceCollection AddAudio(this IServiceCollection services)
    {
        services.AddSingleton<IAudioInputDeviceEnumerator, WasapiAudioInputDeviceEnumerator>();
        services.AddSingleton<IMicrophoneCaptureSessionFactory, WasapiMicrophoneCaptureSessionFactory>();
        services.AddSingleton<MicrophoneCaptureController>();
        services.AddSingleton<IMicrophoneCaptureController>(services => services.GetRequiredService<MicrophoneCaptureController>());
        services.AddSingleton<ISoundPlaybackEngine, WasapiSoundPlaybackEngine>();

        return services;
    }
}

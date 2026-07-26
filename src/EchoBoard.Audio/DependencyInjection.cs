using Microsoft.Extensions.DependencyInjection;
using EchoBoard.Application.Audio;
using EchoBoard.Audio.Microphone;
using EchoBoard.Audio.Playback;
using EchoBoard.Audio.Decoding;
using EchoBoard.Application.Library;

namespace EchoBoard.Audio;

public static class DependencyInjection
{
    public static IServiceCollection AddAudio(this IServiceCollection services)
    {
        services.AddSingleton<IAudioInputDeviceEnumerator, WasapiAudioInputDeviceEnumerator>();
        services.AddSingleton<IMicrophoneCaptureSessionFactory, WasapiMicrophoneCaptureSessionFactory>();
        services.AddSingleton<MicrophoneCaptureController>();
        services.AddSingleton<IMicrophoneCaptureController>(services => services.GetRequiredService<MicrophoneCaptureController>());
        services.AddSingleton<WasapiSoundPlaybackEngine>();
        services.AddSingleton<ISoundPlaybackEngine>(services => services.GetRequiredService<WasapiSoundPlaybackEngine>());
        services.AddSingleton<IAudioRoutingEngine>(services => services.GetRequiredService<WasapiSoundPlaybackEngine>());
        services.AddSingleton<IAudioOutputDeviceEnumerator, WasapiAudioOutputDeviceEnumerator>();
        services.AddScoped<IAudioFileMetadataReader, AudioFileMetadataReader>();

        return services;
    }
}

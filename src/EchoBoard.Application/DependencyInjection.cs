using Microsoft.Extensions.DependencyInjection;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Appearance;
using EchoBoard.Application.Hotkeys;
using EchoBoard.Application.Library;

namespace EchoBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddTransient<CreateSoundUseCase>();
        services.AddTransient<GetSoundUseCase>();
        services.AddTransient<ListSoundsUseCase>();
        services.AddTransient<QuerySoundLibraryUseCase>();
        services.AddTransient<UpdateSoundUseCase>();
        services.AddTransient<SetSoundFavoriteUseCase>();
        services.AddTransient<AssignSoundCategoryUseCase>();
        services.AddTransient<DeleteSoundUseCase>();
        services.AddTransient<ImportSoundsUseCase>();
        services.AddTransient<GenerateSoundWaveformUseCase>();
        services.AddTransient<ListRecentlyPlayedUseCase>();
        services.AddTransient<CreateCategoryUseCase>();
        services.AddTransient<GetCategoryUseCase>();
        services.AddTransient<ListCategoriesUseCase>();
        services.AddTransient<UpdateCategoryUseCase>();
        services.AddTransient<DeleteCategoryUseCase>();
        services.AddTransient<ListHotkeyBindingsUseCase>();
        services.AddTransient<AssignSoundHotkeyUseCase>();
        services.AddTransient<AssignGlobalHotkeyUseCase>();
        services.AddTransient<SetHotkeyBindingEnabledUseCase>();
        services.AddTransient<RemoveHotkeyBindingUseCase>();
        services.AddTransient<RestoreHotkeyBindingsUseCase>();
        services.AddTransient<ListMicrophoneDevicesUseCase>();
        services.AddTransient<LoadMicrophoneSettingsUseCase>();
        services.AddTransient<SelectMicrophoneDeviceUseCase>();
        services.AddTransient<SetMicrophoneGainUseCase>();
        services.AddTransient<SetMicrophoneMuteUseCase>();
        services.AddTransient<StartMicrophoneCaptureUseCase>();
        services.AddTransient<StopMicrophoneCaptureUseCase>();
        services.AddTransient<GetMicrophoneCaptureSnapshotUseCase>();
        services.AddTransient<PlaySoundUseCase>();
        services.AddTransient<LoadAppearanceSettingsUseCase>();
        services.AddTransient<SaveAppearanceSettingsUseCase>();
        services.AddSingleton<HotkeyRuntimeService>();
        services.AddSingleton<IHotkeyRuntimeService>(services => services.GetRequiredService<HotkeyRuntimeService>());

        return services;
    }
}

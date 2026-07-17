using EchoBoard.Application.Audio;

namespace EchoBoard.Application.Appearance;

public static class AppearanceSettingKeys
{
    public const string Theme = "appearance.theme";
    public const string AccentPalette = "appearance.accentPalette";
}

public static class AppearanceThemes
{
    public const string Dark = "Dark";
    public const string Light = "Light";

    public static string Normalize(string? value)
    {
        return string.Equals(value, Light, StringComparison.OrdinalIgnoreCase) ? Light : Dark;
    }
}

public static class AppearancePalettes
{
    public const string Blue = "Blue";
    public const string Cyan = "Cyan";
    public const string Violet = "Violet";
    public const string Emerald = "Emerald";
    public const string Rose = "Rose";

    public static IReadOnlyList<string> All { get; } = [Blue, Cyan, Violet, Emerald, Rose];

    public static string Normalize(string? value)
    {
        return All.FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) ?? Blue;
    }
}

public sealed record AppearanceSettingsDto(string Theme, string AccentPalette)
{
    public static AppearanceSettingsDto Default => new(AppearanceThemes.Dark, AppearancePalettes.Blue);
}

public sealed class LoadAppearanceSettingsUseCase
{
    private readonly IAppSettingRepository settings;

    public LoadAppearanceSettingsUseCase(IAppSettingRepository settings)
    {
        this.settings = settings;
    }

    public async Task<AppearanceSettingsDto> ExecuteAsync(CancellationToken cancellationToken)
    {
        var theme = await settings.GetValueAsync(AppearanceSettingKeys.Theme, cancellationToken);
        var palette = await settings.GetValueAsync(AppearanceSettingKeys.AccentPalette, cancellationToken);

        return new AppearanceSettingsDto(
            AppearanceThemes.Normalize(theme),
            AppearancePalettes.Normalize(palette));
    }
}

public sealed class SaveAppearanceSettingsUseCase
{
    private readonly IAppSettingRepository settings;

    public SaveAppearanceSettingsUseCase(IAppSettingRepository settings)
    {
        this.settings = settings;
    }

    public async Task ExecuteAsync(AppearanceSettingsDto appearance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(appearance);

        await settings.UpsertValueAsync(
            AppearanceSettingKeys.Theme,
            AppearanceThemes.Normalize(appearance.Theme),
            cancellationToken);
        await settings.UpsertValueAsync(
            AppearanceSettingKeys.AccentPalette,
            AppearancePalettes.Normalize(appearance.AccentPalette),
            cancellationToken);
    }
}

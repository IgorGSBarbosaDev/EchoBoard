using EchoBoard.Application.Appearance;
using EchoBoard.Application.Audio;
using FluentAssertions;
using Xunit;

namespace EchoBoard.Application.Tests;

public sealed class AppearanceSettingsUseCaseTests
{
    [Fact]
    public async Task LoadReturnsPersistedAppearance()
    {
        var repository = new FakeAppSettingRepository
        {
            [AppearanceSettingKeys.Theme] = AppearanceThemes.Light,
            [AppearanceSettingKeys.AccentPalette] = AppearancePalettes.Violet
        };

        var result = await new LoadAppearanceSettingsUseCase(repository)
            .ExecuteAsync(TestContext.Current.CancellationToken);

        result.Should().Be(new AppearanceSettingsDto(AppearanceThemes.Light, AppearancePalettes.Violet));
    }

    [Fact]
    public async Task LoadFallsBackToSafeDefaultsForInvalidValues()
    {
        var repository = new FakeAppSettingRepository
        {
            [AppearanceSettingKeys.Theme] = "unknown",
            [AppearanceSettingKeys.AccentPalette] = "neon"
        };

        var result = await new LoadAppearanceSettingsUseCase(repository)
            .ExecuteAsync(TestContext.Current.CancellationToken);

        result.Should().Be(AppearanceSettingsDto.Default);
    }

    [Fact]
    public async Task SaveNormalizesAndPersistsBothSettings()
    {
        var repository = new FakeAppSettingRepository();

        await new SaveAppearanceSettingsUseCase(repository).ExecuteAsync(
            new AppearanceSettingsDto("light", "emerald"),
            TestContext.Current.CancellationToken);

        repository[AppearanceSettingKeys.Theme].Should().Be(AppearanceThemes.Light);
        repository[AppearanceSettingKeys.AccentPalette].Should().Be(AppearancePalettes.Emerald);
    }

    private sealed class FakeAppSettingRepository : IAppSettingRepository
    {
        private readonly Dictionary<string, string> values = [];

        public string? this[string key]
        {
            get => values.GetValueOrDefault(key);
            init
            {
                if (value is not null)
                {
                    values[key] = value;
                }
            }
        }

        public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
        {
            return Task.FromResult(values.GetValueOrDefault(key));
        }

        public Task UpsertValueAsync(string key, string value, CancellationToken cancellationToken)
        {
            values[key] = value;
            return Task.CompletedTask;
        }
    }
}

using EchoBoard.Application.Interfaces;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Hotkeys;
using EchoBoard.Application.Library;
using EchoBoard.App.Navigation;
using EchoBoard.App.ViewModels;
using EchoBoard.App.Views;
using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Xunit;

namespace EchoBoard.App.Tests;

public sealed class ShellNavigationContractTests
{
    [Fact]
    public void NavigationServiceDefaultsToDashboard()
    {
        var service = new NavigationService();

        service.CurrentRoute.Should().Be(ShellRoute.Dashboard);
    }

    [Theory]
    [InlineData(ShellRoute.Dashboard)]
    [InlineData(ShellRoute.Library)]
    [InlineData(ShellRoute.Favorites)]
    [InlineData(ShellRoute.Recent)]
    [InlineData(ShellRoute.Settings)]
    [InlineData(ShellRoute.AudioDiagnostics)]
    public void NavigationServiceTracksSelectedRoute(ShellRoute route)
    {
        var service = new NavigationService();

        service.NavigateTo(route);

        service.CurrentRoute.Should().Be(route);
    }

    [Fact]
    public void MainShellViewModelExposesRequiredNavigationEntries()
    {
        var viewModel = CreateViewModel();

        viewModel.NavigationItems
            .Select(item => item.Label)
            .Should()
            .Equal("Dashboard", "Library", "Favorites", "Recent", "Settings", "Audio Diagnostics");
    }

    [Fact]
    public void MainShellViewModelDefaultsToDashboardPage()
    {
        var viewModel = CreateViewModel();

        viewModel.SelectedNavigationItem.Route.Should().Be(ShellRoute.Dashboard);
        viewModel.CurrentPage.Should().BeOfType<DashboardViewModel>();
    }

    [Fact]
    public void MainShellViewModelNavigatesToSettings()
    {
        var viewModel = CreateViewModel();

        viewModel.NavigateCommand.Execute(ShellRoute.Settings);

        viewModel.SelectedNavigationItem.Route.Should().Be(ShellRoute.Settings);
        viewModel.CurrentPage.Should().BeOfType<SettingsViewModel>();
    }

    [Fact]
    public void ShellPageTemplateSelectorOverridesContentControlSelectionOverload()
    {
        var overload = typeof(ShellPageTemplateSelector).GetMethod(
            "SelectTemplateCore",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            [typeof(object), typeof(DependencyObject)],
            modifiers: null);

        overload.Should().NotBeNull();
        overload!.DeclaringType.Should().Be<ShellPageTemplateSelector>();
    }

    [Fact]
    public void MainShellViewModelCollapsesContextPanelWithoutReservingPanelWidth()
    {
        var viewModel = CreateViewModel();

        viewModel.ToggleContextPanelCommand.Execute(null);

        viewModel.IsContextPanelOpen.Should().BeFalse();
        viewModel.ContextPanelColumnWidth.Value.Should().Be(0);
        viewModel.ContextPanelVisibility.Should().Be(Microsoft.UI.Xaml.Visibility.Collapsed);
        viewModel.ContextPanelExpandButtonVisibility.Should().Be(Microsoft.UI.Xaml.Visibility.Visible);
    }

    [Fact]
    public void AppHostRegistersShellDependencies()
    {
        using var host = Hosting.AppHost.Create();

        host.Services.GetRequiredService<INavigationService>().Should().NotBeNull();
        host.Services.GetRequiredService<MainShellViewModel>().Should().NotBeNull();
        host.Services.GetRequiredService<IDatabaseInitializer>().Should().NotBeNull();
    }

    [Fact]
    public async Task AppHostInitializesDatabaseOnce()
    {
        var initializer = new FakeDatabaseInitializer();
        var services = new ServiceCollection()
            .AddScoped<IDatabaseInitializer>(_ => initializer)
            .BuildServiceProvider();

        await Hosting.AppHost.InitializeDatabaseAsync(services, TestContext.Current.CancellationToken);

        initializer.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task AppHostRethrowsDatabaseInitializationFailures()
    {
        var expected = new InvalidOperationException("database unavailable");
        var services = new ServiceCollection()
            .AddScoped<IDatabaseInitializer>(_ => new FakeDatabaseInitializer(expected))
            .BuildServiceProvider();

        Func<Task> act = () => Hosting.AppHost.InitializeDatabaseAsync(services, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("database unavailable");
    }

    private static MainShellViewModel CreateViewModel()
    {
        return new MainShellViewModel(
            new NavigationService(),
            new DashboardViewModel(),
            CreateLibraryViewModel(),
            CreateFavoritesViewModel(),
            new RecentViewModel(),
            CreateSettingsViewModel(),
            CreateAudioDiagnosticsViewModel());
    }

    private static LibraryViewModel CreateLibraryViewModel()
    {
        var sounds = new FakeSoundLibraryRepository();
        var categories = new FakeCategoryRepository();
        var files = new FakeSoundFileAvailabilityReader();
        var hotkeys = new FakeHotkeyBindingRepository();
        var runtime = new FakeHotkeyRuntime();

        return new LibraryViewModel(
            new QuerySoundLibraryUseCase(sounds, categories, files),
            new ImportSoundsUseCase(sounds, new FakeAudioFileMetadataReader()),
            new CreateCategoryUseCase(categories),
            new UpdateCategoryUseCase(categories),
            new DeleteCategoryUseCase(categories),
            new SetSoundFavoriteUseCase(sounds),
            new AssignSoundCategoryUseCase(sounds, categories),
            new ListHotkeyBindingsUseCase(hotkeys, runtime),
            new AssignSoundHotkeyUseCase(hotkeys, sounds, runtime),
            new RemoveHotkeyBindingUseCase(hotkeys, runtime),
            new SetHotkeyBindingEnabledUseCase(hotkeys, runtime));
    }

    private static SettingsViewModel CreateSettingsViewModel()
    {
        var hotkeys = new FakeHotkeyBindingRepository();
        var runtime = new FakeHotkeyRuntime();
        var settings = new FakeAppSettingRepository();
        var microphone = new FakeMicrophoneCaptureController();

        return new SettingsViewModel(
            new ListHotkeyBindingsUseCase(hotkeys, runtime),
            new AssignGlobalHotkeyUseCase(hotkeys, runtime),
            new RemoveHotkeyBindingUseCase(hotkeys, runtime),
            new SetHotkeyBindingEnabledUseCase(hotkeys, runtime),
            new ListMicrophoneDevicesUseCase(microphone),
            new LoadMicrophoneSettingsUseCase(settings, microphone),
            new SelectMicrophoneDeviceUseCase(settings, microphone),
            new SetMicrophoneGainUseCase(settings, microphone),
            new SetMicrophoneMuteUseCase(settings, microphone),
            new StartMicrophoneCaptureUseCase(microphone),
            new StopMicrophoneCaptureUseCase(microphone),
            new GetMicrophoneCaptureSnapshotUseCase(microphone));
    }

    private static AudioDiagnosticsViewModel CreateAudioDiagnosticsViewModel()
    {
        return new AudioDiagnosticsViewModel(new GetMicrophoneCaptureSnapshotUseCase(new FakeMicrophoneCaptureController()));
    }

    private static FavoritesViewModel CreateFavoritesViewModel()
    {
        var sounds = new FakeSoundLibraryRepository();
        var categories = new FakeCategoryRepository();
        var files = new FakeSoundFileAvailabilityReader();

        return new FavoritesViewModel(
            new QuerySoundLibraryUseCase(sounds, categories, files),
            new SetSoundFavoriteUseCase(sounds));
    }

    private sealed class FakeDatabaseInitializer : IDatabaseInitializer
    {
        private readonly Exception? exception;

        public FakeDatabaseInitializer(Exception? exception = null)
        {
            this.exception = exception;
        }

        public int CallCount { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            return exception is null ? Task.CompletedTask : Task.FromException(exception);
        }
    }

    private sealed class FakeSoundLibraryRepository : ISoundLibraryRepository
    {
        public Task<IReadOnlyList<Sound>> ListSoundsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Sound>>([]);
        }

        public Task<Sound?> GetSoundAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult<Sound?>(null);
        }

        public Task<bool> SoundFilePathExistsAsync(string filePath, Guid? excludingSoundId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task AddSoundAsync(Sound sound, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateSoundAsync(Sound sound, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DeleteSoundAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAudioFileMetadataReader : IAudioFileMetadataReader
    {
        public Task<AudioFileMetadata> ReadAsync(string filePath, CancellationToken cancellationToken)
        {
            throw new AudioFileMetadataException(filePath, "Audio metadata could not be read.");
        }
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public Task<IReadOnlyList<Category>> ListCategoriesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Category>>([]);
        }

        public Task<Category?> GetCategoryAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult<Category?>(null);
        }

        public Task<bool> CategoryNameExistsAsync(string name, Guid? excludingCategoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task AddCategoryAsync(Category category, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateCategoryAsync(Category category, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSoundFileAvailabilityReader : ISoundFileAvailabilityReader
    {
        public Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakeHotkeyBindingRepository : IHotkeyBindingRepository
    {
        public Task<IReadOnlyList<HotkeyBinding>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<HotkeyBinding>>([]);
        }

        public Task<HotkeyBinding?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult<HotkeyBinding?>(null);
        }

        public Task<HotkeyBinding?> GetForSoundAsync(Guid soundId, CancellationToken cancellationToken)
        {
            return Task.FromResult<HotkeyBinding?>(null);
        }

        public Task<HotkeyBinding?> GetForGlobalCommandAsync(GlobalHotkeyCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult<HotkeyBinding?>(null);
        }

        public Task<bool> CombinationExistsAsync(string normalizedKeyCombination, Guid? excludingBindingId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task AddAsync(HotkeyBinding binding, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(HotkeyBinding binding, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHotkeyRuntime : IHotkeyRuntimeService
    {
        public Task<HotkeyRegistrationResult> RegisterBindingAsync(HotkeyBinding binding, CancellationToken cancellationToken)
        {
            return Task.FromResult(HotkeyRegistrationResult.Active("Registered."));
        }

        public Task UnregisterBindingAsync(Guid bindingId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public HotkeyRegistrationState GetRegistrationState(Guid bindingId)
        {
            return HotkeyRegistrationState.Active;
        }
    }

    private sealed class FakeAppSettingRepository : IAppSettingRepository
    {
        public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(null);
        }

        public Task UpsertValueAsync(string key, string value, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMicrophoneCaptureController : IMicrophoneCaptureController
    {
        public IMicrophonePcmSource? CurrentSource => null;

        public Task<IReadOnlyList<AudioInputDeviceDto>> ListInputDevicesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AudioInputDeviceDto>>([]);
        }

        public Task RestoreSelectionAsync(MicrophoneSettingsDto settings, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SelectDeviceAsync(AudioInputDeviceDto device, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SetGainAsync(double gain, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SetMutedAsync(bool isMuted, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public MicrophoneCaptureSnapshot GetSnapshot()
        {
            return MicrophoneCaptureSnapshot.Unavailable("No microphone available. Connect an input device.", MicrophoneSettingsDto.Default);
        }
    }
}

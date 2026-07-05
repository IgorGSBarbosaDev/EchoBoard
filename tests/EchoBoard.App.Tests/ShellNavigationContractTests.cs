using EchoBoard.Application.Interfaces;
using EchoBoard.Application.Library;
using EchoBoard.App.Navigation;
using EchoBoard.App.ViewModels;
using EchoBoard.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
            new SettingsViewModel(),
            new AudioDiagnosticsViewModel());
    }

    private static LibraryViewModel CreateLibraryViewModel()
    {
        var sounds = new FakeSoundLibraryRepository();
        var categories = new FakeCategoryRepository();
        var files = new FakeSoundFileAvailabilityReader();

        return new LibraryViewModel(
            new QuerySoundLibraryUseCase(sounds, categories, files),
            new ImportSoundsUseCase(sounds, new FakeAudioFileMetadataReader()),
            new CreateCategoryUseCase(categories),
            new UpdateCategoryUseCase(categories),
            new DeleteCategoryUseCase(categories),
            new SetSoundFavoriteUseCase(sounds),
            new AssignSoundCategoryUseCase(sounds, categories));
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
}

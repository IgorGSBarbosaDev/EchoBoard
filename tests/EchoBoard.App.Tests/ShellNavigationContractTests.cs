using EchoBoard.App.Navigation;
using EchoBoard.App.ViewModels;
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
    }

    private static MainShellViewModel CreateViewModel()
    {
        return new MainShellViewModel(
            new NavigationService(),
            new DashboardViewModel(),
            new LibraryViewModel(),
            new FavoritesViewModel(),
            new RecentViewModel(),
            new SettingsViewModel(),
            new AudioDiagnosticsViewModel());
    }
}

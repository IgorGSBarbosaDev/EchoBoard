using FluentAssertions;
using Xunit;

namespace EchoBoard.App.Tests;

public sealed class DashboardLayoutContractTests
{
    [Fact]
    public void DashboardContainsOnlyTheCurrentHomeSectionsInOrder()
    {
        var dashboard = File.ReadAllText(Path.Combine(AppPath(), "Views", "DashboardPage.xaml"));

        dashboard.Should().NotContain("VISÃO GERAL");
        dashboard.Should().NotContain("Seu painel de áudio");
        dashboard.Should().NotContain("Acompanhe a biblioteca");
        dashboard.Should().NotContain("Primeiros passos");
        dashboard.Should().NotContain("SummaryGrid");
        dashboard.Should().NotContain("SideColumn");
        dashboard.Should().NotContain("SideContent");
        dashboard.Should().NotContain("SetupSteps");
        dashboard.Should().NotContain("FlowColumn");
        dashboard.Should().NotContain("FlowArrow");
        dashboard.Should().Contain("x:Name=\"AudioSectionsLayout\"");
        dashboard.Should().Contain("<ColumnDefinition Width=\"1.5*\" />");
        dashboard.Should().Contain("<ColumnDefinition Width=\"1*\" />");
        dashboard.Should().Contain("x:Name=\"FlowAudioPanel\"");
        dashboard.Should().Contain("x:Name=\"LiveLevelsPanel\"");
        dashboard.Should().Contain("<AdaptiveTrigger MinWindowWidth=\"960\" />");
        dashboard.Should().Contain("<AdaptiveTrigger MinWindowWidth=\"0\" />");
        dashboard.Should().Contain("Target=\"LiveLevelsPanel.(Grid.Row)\"");

        dashboard.IndexOf("Text=\"Início\"", StringComparison.Ordinal).Should().BeLessThan(dashboard.IndexOf("Text=\"Acesso rápido\"", StringComparison.Ordinal));
        dashboard.IndexOf("Text=\"Acesso rápido\"", StringComparison.Ordinal).Should().BeLessThan(dashboard.IndexOf("Text=\"Fluxo de áudio\"", StringComparison.Ordinal));
        dashboard.IndexOf("Text=\"Fluxo de áudio\"", StringComparison.Ordinal).Should().BeLessThan(dashboard.IndexOf("Text=\"Níveis ao vivo\"", StringComparison.Ordinal));
    }

    [Fact]
    public void DashboardViewModelDoesNotKeepRemovedDashboardState()
    {
        var viewModel = File.ReadAllText(Path.Combine(AppPath(), "ViewModels", "DashboardViewModel.cs"));

        viewModel.Should().NotContain("LibraryValue");
        viewModel.Should().NotContain("LibraryNote");
        viewModel.Should().NotContain("HotkeyValue");
        viewModel.Should().NotContain("HotkeyNote");
        viewModel.Should().NotContain("MicrophoneValue");
        viewModel.Should().NotContain("RoutingValue");
        viewModel.Should().NotContain("RoutingNote");
        viewModel.Should().NotContain("DashboardSetupStepViewModel");
        viewModel.Should().NotContain("ReplaceSetupSteps");
    }

    private static string AppPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EchoBoard.sln")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(
            directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root."),
            "src",
            "EchoBoard.App");
    }
}

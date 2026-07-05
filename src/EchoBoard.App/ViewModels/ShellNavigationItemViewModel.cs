using EchoBoard.App.Navigation;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.ViewModels;

public sealed class ShellNavigationItemViewModel
{
    public ShellNavigationItemViewModel(
        ShellRoute route,
        string label,
        Symbol icon,
        string automationName)
    {
        Route = route;
        Label = label;
        Icon = icon;
        AutomationName = automationName;
    }

    public ShellRoute Route { get; }

    public string Label { get; }

    public Symbol Icon { get; }

    public string AutomationName { get; }
}

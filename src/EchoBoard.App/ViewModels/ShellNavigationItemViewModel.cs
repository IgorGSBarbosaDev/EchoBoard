using CommunityToolkit.Mvvm.ComponentModel;
using EchoBoard.App.Navigation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace EchoBoard.App.ViewModels;

public sealed class ShellNavigationItemViewModel : ObservableObject
{
    private bool isLabelVisible = true;

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

    public bool IsLabelVisible
    {
        get => isLabelVisible;
        set
        {
            if (SetProperty(ref isLabelVisible, value))
            {
                OnPropertyChanged(nameof(LabelVisibility));
                OnPropertyChanged(nameof(ContentWidth));
                OnPropertyChanged(nameof(ContentAlignment));
            }
        }
    }

    public Visibility LabelVisibility => IsLabelVisible ? Visibility.Visible : Visibility.Collapsed;

    public double ContentWidth => IsLabelVisible ? 176 : 40;

    public HorizontalAlignment ContentAlignment => IsLabelVisible
        ? HorizontalAlignment.Left
        : HorizontalAlignment.Center;
}

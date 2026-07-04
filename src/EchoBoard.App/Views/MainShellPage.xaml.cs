using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Views;

public sealed partial class MainShellPage : Page
{
    public MainShellPage(MainShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

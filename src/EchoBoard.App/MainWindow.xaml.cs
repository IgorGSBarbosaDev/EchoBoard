using EchoBoard.App.Views;
using Microsoft.UI.Xaml;

namespace EchoBoard.App;

public sealed partial class MainWindow : Window
{
    public MainWindow(MainShellPage shellPage)
    {
        InitializeComponent();
        Content = shellPage;
    }
}

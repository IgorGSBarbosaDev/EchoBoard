using CommunityToolkit.Mvvm.ComponentModel;

namespace EchoBoard.App.ViewModels;

public sealed partial class MainShellViewModel : ObservableObject
{
    private string title = "EchoBoard";

    public string Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }
}

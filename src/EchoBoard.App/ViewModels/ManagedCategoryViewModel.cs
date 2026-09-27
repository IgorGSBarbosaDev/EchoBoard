using System.Windows.Input;

namespace EchoBoard.App.ViewModels;

public sealed record ManagedCategoryViewModel(
    Guid Id,
    string Name,
    string CountText,
    ICommand EditCommand,
    ICommand DeleteCommand)
{
    public string EditAutomationName => $"Edit {Name}";

    public string DeleteAutomationName => $"Delete {Name}";
}

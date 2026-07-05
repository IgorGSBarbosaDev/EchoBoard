namespace EchoBoard.App.Navigation;

public interface INavigationService
{
    event EventHandler<ShellRoute>? RouteChanged;

    ShellRoute CurrentRoute { get; }

    void NavigateTo(ShellRoute route);
}

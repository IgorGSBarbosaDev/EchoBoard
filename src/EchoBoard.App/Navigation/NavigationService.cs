namespace EchoBoard.App.Navigation;

public sealed class NavigationService : INavigationService
{
    public event EventHandler<ShellRoute>? RouteChanged;

    public ShellRoute CurrentRoute { get; private set; } = ShellRoute.Dashboard;

    public void NavigateTo(ShellRoute route)
    {
        if (CurrentRoute == route)
        {
            return;
        }

        CurrentRoute = route;
        RouteChanged?.Invoke(this, route);
    }
}

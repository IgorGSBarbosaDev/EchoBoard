using CommunityToolkit.Mvvm.ComponentModel;

namespace EchoBoard.App.ViewModels;

public sealed class FavoritesViewModel : ObservableObject
{
    public string Title => "Favorites";

    public string Subtitle => "Fast access area for frequently used sounds.";

    public string EmptyStateTitle => "No favorites yet";

    public string EmptyStateMessage => "Sounds marked as favorites will be collected here.";
}

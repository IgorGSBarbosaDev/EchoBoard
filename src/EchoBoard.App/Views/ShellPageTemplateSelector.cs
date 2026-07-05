using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Views;

public sealed class ShellPageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DashboardTemplate { get; set; }

    public DataTemplate? LibraryTemplate { get; set; }

    public DataTemplate? FavoritesTemplate { get; set; }

    public DataTemplate? RecentTemplate { get; set; }

    public DataTemplate? SettingsTemplate { get; set; }

    public DataTemplate? AudioDiagnosticsTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return item switch
        {
            DashboardViewModel => DashboardTemplate,
            LibraryViewModel => LibraryTemplate,
            FavoritesViewModel => FavoritesTemplate,
            RecentViewModel => RecentTemplate,
            SettingsViewModel => SettingsTemplate,
            AudioDiagnosticsViewModel => AudioDiagnosticsTemplate,
            _ => base.SelectTemplateCore(item)
        };
    }
}

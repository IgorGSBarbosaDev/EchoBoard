using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Views;

public sealed class ShellPageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? LibraryTemplate { get; set; }

    public DataTemplate? SettingsTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return SelectTemplateFor(item) ?? base.SelectTemplateCore(item);
    }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateFor(item) ?? base.SelectTemplateCore(item, container);
    }

    private DataTemplate? SelectTemplateFor(object item)
    {
        return item switch
        {
            LibraryViewModel => LibraryTemplate,
            SettingsViewModel => SettingsTemplate,
            _ => null
        };
    }
}

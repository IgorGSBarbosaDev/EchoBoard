using Microsoft.UI.Xaml;

namespace EchoBoard.App.Appearance;

public interface IAppearanceResourceManager
{
    void Apply(string palette, ElementTheme theme);
}

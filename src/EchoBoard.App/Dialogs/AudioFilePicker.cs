using Windows.Storage.Pickers;
using WinRT.Interop;

namespace EchoBoard.App.Dialogs;

public static class AudioFilePicker
{
    public static async Task<IReadOnlyList<string>> PickMultipleAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary
        };

        foreach (var extension in new[] { ".mp3", ".wav", ".ogg", ".flac", ".m4a", ".aac" })
        {
            picker.FileTypeFilter.Add(extension);
        }

        if (MainWindow.CurrentInstance is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(MainWindow.CurrentInstance));
        }

        var files = await picker.PickMultipleFilesAsync();
        return files.Select(file => file.Path).Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
    }
}

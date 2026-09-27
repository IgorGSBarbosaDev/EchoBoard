using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace EchoBoard.App.ViewModels;

public sealed class CategorySoundSelectionViewModel : ObservableObject
{
    private bool isSelected;
    private bool wasSelected;

    public CategorySoundSelectionViewModel(
        Guid id,
        string name,
        IReadOnlyCollection<Guid> originalCategoryIds,
        Guid? editorCategoryId,
        bool isMissingFile,
        ICommand previewCommand)
    {
        Id = id;
        Name = name;
        wasSelected = editorCategoryId is Guid categoryId && originalCategoryIds.Contains(categoryId);
        isSelected = wasSelected;
        IsMissingFile = isMissingFile;
        PreviewCommand = previewCommand;
    }

    public Guid Id { get; }

    public string Name { get; }

    public bool IsMissingFile { get; }

    public bool CanPreview => !IsMissingFile;

    public string SelectionAutomationName => IsSelected
        ? $"Remove {Name} from category"
        : $"Add {Name} to category";

    public string PreviewAutomationName => $"Play {Name}";

    public ICommand PreviewCommand { get; }

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (SetProperty(ref isSelected, value))
            {
                OnPropertyChanged(nameof(SelectionAutomationName));
            }
        }
    }

    public bool WasSelected => wasSelected;

    public void MarkPersistedSelection()
    {
        wasSelected = IsSelected;
    }
}

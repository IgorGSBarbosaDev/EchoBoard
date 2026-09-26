using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace EchoBoard.App.ViewModels;

public sealed class CategorySoundSelectionViewModel : ObservableObject
{
    private bool isSelected;
    private Guid? originalCategoryId;

    public CategorySoundSelectionViewModel(
        Guid id,
        string name,
        Guid? originalCategoryId,
        Guid? editorCategoryId,
        bool isMissingFile,
        ICommand previewCommand)
    {
        Id = id;
        Name = name;
        this.originalCategoryId = originalCategoryId;
        isSelected = originalCategoryId == editorCategoryId;
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

    public Guid? OriginalCategoryId => originalCategoryId;

    public void MarkPersistedCategory(Guid? categoryId)
    {
        originalCategoryId = categoryId;
    }
}

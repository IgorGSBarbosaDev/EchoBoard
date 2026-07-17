using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Library;
using Microsoft.UI.Xaml;

namespace EchoBoard.App.ViewModels;

public sealed class RecentViewModel : ObservableObject
{
    private readonly ListRecentlyPlayedUseCase listRecent;
    private readonly PlaySoundUseCase playSound;
    private readonly SoundDetailsViewModel details;

    public RecentViewModel(ListRecentlyPlayedUseCase listRecent, PlaySoundUseCase playSound, SoundDetailsViewModel details)
    {
        this.listRecent = listRecent;
        this.playSound = playSound;
        this.details = details;
        Sounds = [];
        details.SoundChanged += OnSoundChanged;
    }

    public string Title => "Recentes";
    public string Subtitle => "Sons reproduzidos recentemente neste dispositivo.";
    public ObservableCollection<RecentSoundViewModel> Sounds { get; }
    public Visibility EmptyVisibility => Sounds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ListVisibility => Sounds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var items = await listRecent.ExecuteAsync(50, cancellationToken);
        Sounds.Clear();
        foreach (var item in items)
        {
            Sounds.Add(new RecentSoundViewModel(
                item.Sound.Id,
                item.Sound.Name,
                item.Sound.Extension.TrimStart('.').ToUpperInvariant(),
                item.PlayedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                new AsyncRelayCommand(_ => PlayAsync(item.Sound.Id, CancellationToken.None)),
                details.OpenCommand));
        }

        OnPropertyChanged(nameof(EmptyVisibility));
        OnPropertyChanged(nameof(ListVisibility));
    }

    private async Task PlayAsync(Guid soundId, CancellationToken cancellationToken)
    {
        await playSound.ExecuteAsync(new PlaySoundRequest(soundId, DateTimeOffset.UtcNow), cancellationToken);
        await LoadAsync(cancellationToken);
    }

    private async void OnSoundChanged(object? sender, EventArgs e)
    {
        await LoadAsync(CancellationToken.None);
    }
}

public sealed record RecentSoundViewModel(Guid Id, string Title, string Format, string PlayedAt, IAsyncRelayCommand PlayCommand, IAsyncRelayCommand<Guid> DetailsCommand);

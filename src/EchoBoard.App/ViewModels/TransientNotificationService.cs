using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.App.Controls;
using Microsoft.UI.Xaml;

namespace EchoBoard.App.ViewModels;

public sealed class TransientNotificationService : ObservableObject, IDisposable
{
    private readonly object sync = new();
    private CancellationTokenSource? dismissal;
    private ToastPreviewModel? current;

    public TransientNotificationService()
    {
        DismissCommand = new RelayCommand(Dismiss);
    }

    public ToastPreviewModel? Current
    {
        get => current;
        private set
        {
            if (SetProperty(ref current, value))
            {
                OnPropertyChanged(nameof(Visibility));
            }
        }
    }

    public Visibility Visibility => Current is null ? Visibility.Collapsed : Visibility.Visible;

    public IRelayCommand DismissCommand { get; }

    public void Show(ToastNotificationKind kind, string title, string description)
    {
        CancellationToken token;
        lock (sync)
        {
            dismissal?.Cancel();
            dismissal?.Dispose();
            dismissal = new CancellationTokenSource();
            token = dismissal.Token;
        }

        Current = new ToastPreviewModel(kind, title, description);
        _ = DismissLaterAsync(token);
    }

    public void Dismiss()
    {
        lock (sync)
        {
            dismissal?.Cancel();
            dismissal?.Dispose();
            dismissal = null;
        }

        Current = null;
    }

    public void Dispose()
    {
        lock (sync)
        {
            dismissal?.Cancel();
            dismissal?.Dispose();
            dismissal = null;
        }
    }

    private async Task DismissLaterAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            Current = null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.Application.Audio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchoBoard.App.ViewModels;

public sealed class AudioRoutingSettingsCoordinator : ObservableObject, IDisposable
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IAudioRoutingEngine engine;
    private readonly ILogger<AudioRoutingSettingsCoordinator> logger;
    private readonly SemaphoreSlim loadGate = new(1, 1);
    private readonly object saveSync = new();
    private readonly object applySync = new();
    private Task saveTask = Task.CompletedTask;
    private Task applyTask = Task.CompletedTask;
    private AudioRoutingSettingsDto current = AudioRoutingSettingsDto.Default;
    private bool isLoaded;
    private bool disposed;

    public AudioRoutingSettingsCoordinator(
        IServiceScopeFactory scopeFactory,
        IAudioRoutingEngine engine,
        ILogger<AudioRoutingSettingsCoordinator> logger)
    {
        this.scopeFactory = scopeFactory;
        this.engine = engine;
        this.logger = logger;
        ToggleMicrophoneMuteCommand = new RelayCommand(() => Update(value => value with { IsMicrophoneMuted = !value.IsMicrophoneMuted }));
        ToggleEffectsMuteCommand = new RelayCommand(() => Update(value => value with { AreEffectsMuted = !value.AreEffectsMuted }));
        ToggleMonitorMuteCommand = new RelayCommand(() => Update(value => value with { IsMonitorMuted = !value.IsMonitorMuted }));
        ToggleVirtualOutputMuteCommand = new RelayCommand(() => Update(value => value with { IsVirtualOutputMuted = !value.IsVirtualOutputMuted }));
    }

    public AudioRoutingSettingsDto Current
    {
        get => current;
        private set
        {
            if (SetProperty(ref current, value))
            {
                NotifyAll();
            }
        }
    }

    public double MicrophonePercent
    {
        get => Current.MicrophoneVolume * 100;
        set => Update(settings => settings with { MicrophoneVolume = Percent(value) });
    }

    public double EffectsPercent
    {
        get => Current.EffectsVolume * 100;
        set => Update(settings => settings with { EffectsVolume = Percent(value) });
    }

    public double MonitorPercent
    {
        get => Current.MonitorVolume * 100;
        set => Update(settings => settings with { MonitorVolume = Percent(value) });
    }

    public double VirtualOutputPercent
    {
        get => Current.VirtualOutputVolume * 100;
        set => Update(settings => settings with { VirtualOutputVolume = Percent(value) });
    }

    public bool IsMicrophoneMuted
    {
        get => Current.IsMicrophoneMuted;
        set => Update(settings => settings with { IsMicrophoneMuted = value });
    }

    public bool AreEffectsMuted
    {
        get => Current.AreEffectsMuted;
        set => Update(settings => settings with { AreEffectsMuted = value });
    }

    public bool IsMonitorEnabled
    {
        get => Current.IsMonitorEnabled;
        set => Update(settings => settings with { IsMonitorEnabled = value });
    }

    public bool IsMonitorMuted
    {
        get => Current.IsMonitorMuted;
        set => Update(settings => settings with { IsMonitorMuted = value });
    }

    public bool IsVirtualOutputMuted
    {
        get => Current.IsVirtualOutputMuted;
        set => Update(settings => settings with { IsVirtualOutputMuted = value });
    }

    public string MicrophonePercentText => $"{MicrophonePercent:0}%";
    public string EffectsPercentText => $"{EffectsPercent:0}%";
    public string MonitorPercentText => $"{MonitorPercent:0}%";
    public string VirtualOutputPercentText => $"{VirtualOutputPercent:0}%";

    public IRelayCommand ToggleMicrophoneMuteCommand { get; }
    public IRelayCommand ToggleEffectsMuteCommand { get; }
    public IRelayCommand ToggleMonitorMuteCommand { get; }
    public IRelayCommand ToggleVirtualOutputMuteCommand { get; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (isLoaded)
        {
            return;
        }

        await loadGate.WaitAsync(cancellationToken);
        try
        {
            if (isLoaded)
            {
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            Current = await scope.ServiceProvider
                .GetRequiredService<LoadAudioRoutingSettingsUseCase>()
                .ExecuteAsync(cancellationToken);
            isLoaded = true;
        }
        finally
        {
            loadGate.Release();
        }
    }

    public void SetInputDevice(string? id, string? name)
    {
        Update(settings => settings with { InputDeviceId = id, InputDeviceName = name });
    }

    public void SetMonitorDevice(string? id, string? name)
    {
        Update(settings => settings with { MonitorDeviceId = id, MonitorDeviceName = name });
    }

    public void SetVirtualOutputDevice(string? id, string? name)
    {
        Update(settings => settings with { VirtualOutputDeviceId = id, VirtualOutputDeviceName = name });
    }

    public Task FlushAsync() => Task.WhenAll(applyTask, saveTask);

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        loadGate.Dispose();
    }

    private void Update(Func<AudioRoutingSettingsDto, AudioRoutingSettingsDto> update)
    {
        var next = Validate(update(Current));
        if (next == Current)
        {
            return;
        }

        Current = next;
        Task latestApply;
        lock (applySync)
        {
            applyTask = ApplyAfterAsync(applyTask, next);
            latestApply = applyTask;
        }
        lock (saveSync)
        {
            saveTask = SaveAfterAsync(saveTask, latestApply, next);
        }
    }

    private async Task ApplyAfterAsync(Task previous, AudioRoutingSettingsDto value)
    {
        try
        {
            await previous;
            await engine.ApplySettingsAsync(value, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Audio routing settings could not be applied.");
        }
    }

    private async Task SaveAfterAsync(Task previous, Task applied, AudioRoutingSettingsDto value)
    {
        try
        {
            await previous;
        }
        catch (Exception)
        {
            // The latest complete snapshot can repair an earlier persistence failure.
        }

        try
        {
            await applied;
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider
                .GetRequiredService<SaveAudioRoutingSettingsUseCase>()
                .ExecuteAsync(value, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Audio routing settings could not be persisted.");
        }
    }

    private void NotifyAll()
    {
        foreach (var propertyName in new[]
                 {
                     nameof(MicrophonePercent), nameof(EffectsPercent), nameof(MonitorPercent),
                     nameof(VirtualOutputPercent), nameof(MicrophonePercentText), nameof(EffectsPercentText),
                     nameof(MonitorPercentText), nameof(VirtualOutputPercentText), nameof(IsMicrophoneMuted),
                     nameof(AreEffectsMuted), nameof(IsMonitorEnabled), nameof(IsMonitorMuted),
                     nameof(IsVirtualOutputMuted)
                 })
        {
            OnPropertyChanged(propertyName);
        }
    }

    private static double Percent(double value) => Math.Clamp(value, 0, 100) / 100.0;

    private static AudioRoutingSettingsDto Validate(AudioRoutingSettingsDto value)
    {
        return value with
        {
            MicrophoneVolume = Math.Clamp(value.MicrophoneVolume, 0, 1),
            EffectsVolume = Math.Clamp(value.EffectsVolume, 0, 1),
            MonitorVolume = Math.Clamp(value.MonitorVolume, 0, 1),
            VirtualOutputVolume = Math.Clamp(value.VirtualOutputVolume, 0, 1)
        };
    }
}

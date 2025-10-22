using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Models;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Services.Dialog;
using ReOsuStoryboardPlayer.Avalonia.Services.Persistences;
using ReOsuStoryboardPlayer.Avalonia.Services.Render;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Services.Window;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages.Play;

public partial class PlayPageViewModel : PageViewModelBase
{
    private readonly IAudioManager audioManager;
    private readonly IDialogManager dialogManager;
    private readonly ILogger<PlayPageViewModel> logger;
    private readonly IPersistence persistence;
    private readonly IRenderManager renderManager;
    private readonly IStoryboardLoadDialog storyboardLoadDialog;
    private readonly IWindowManager windowManager;

    [ObservableProperty]
    private IAudioPlayer audioPlayer;

    [ObservableProperty]
    private bool isControlPanelVisible = true;

    [ObservableProperty]
    private StoryboardInstance storyboardInstance;

    [ObservableProperty]
    private StoryboardPlayerSetting storyboardPlayerSetting;

    [ObservableProperty]
    private float storyboardPlayTime;

    private IDisposable timerDispose;

    public PlayPageViewModel(
        IDialogManager dialogManager,
        IStoryboardLoadDialog storyboardLoadDialog,
        IPersistence persistence,
        ILogger<PlayPageViewModel> logger,
        IWindowManager windowManager, IRenderManager renderManager,
        IAudioManager audioManager)
    {
        this.dialogManager = dialogManager;
        this.storyboardLoadDialog = storyboardLoadDialog;
        this.persistence = persistence;
        this.logger = logger;
        this.windowManager = windowManager;
        this.renderManager = renderManager;
        this.audioManager = audioManager;

        Initialize();
    }

    public override string Title => "主页";

    public TimeSpan CurrentAudioTime => AudioPlayer?.CurrentTime ?? TimeSpan.Zero;

    private async void Initialize()
    {
        StoryboardPlayerSetting =
            await persistence.Load(StoryboardPlayerSetting.JsonTypeInfo);
    }

    partial void OnStoryboardInstanceChanged(StoryboardInstance value)
    {
        UpdateTitle();
    }

    partial void OnAudioPlayerChanged(IAudioPlayer oldValue, IAudioPlayer newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= OnAudioPlayerPropertyChanged;
        if (newValue != null)
            newValue.PropertyChanged += OnAudioPlayerPropertyChanged;
    }

    private void OnAudioPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IAudioPlayer.IsPlaying):
                UpdateTitle();
                break;
        }
    }

    private void UpdateTitle()
    {
        windowManager.MainWindowTitle =
            $"{(AudioPlayer?.IsPlaying ?? false ? "Playing" : "Paused")} {StoryboardInstance?.StoryboardInfo.BeatmapSetId} {StoryboardInstance?.StoryboardInfo.Artist} - {StoryboardInstance?.StoryboardInfo.Title} [{StoryboardInstance?.StoryboardInfo.DifficultyName}]";
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task LoadStoryboardInstance(CancellationToken token = default)
    {
        try
        {
            var instance = await storyboardLoadDialog.OpenLoaderDialog();
            if (instance is null || token.IsCancellationRequested)
                return;

            logger.LogInformationEx($"user loaded storyboard instance: {instance}");
            Stop();

            var oldAudioPlayer = AudioPlayer;
            AudioPlayer = await audioManager.LoadAudio(instance);
            logger.LogInformationEx("Begin dispose old audio player.");
            oldAudioPlayer?.Dispose();

            var oldInstance = StoryboardInstance;
            StoryboardInstance = instance;
            logger.LogInformationEx("Begin dispose old storyboard instance.");
            oldInstance.Dispose();

            StoryboardPlayTime = -(float) AudioPlayer.LeadIn.TotalMilliseconds;
        }
        catch (Exception e)
        {
            var msg = e.Message;
            logger.LogErrorEx(e, $"open storyboard dialog exception: {e.Message}");
            await dialogManager.ShowMessageDialog(msg, DialogMessageType.Error);
        }
    }

    [RelayCommand]
    private void Play()
    {
        AudioPlayer.Play();
        timerDispose?.Dispose();
        timerDispose = DispatcherTimer.Run(() =>
            {
                OnPropertyChanged(nameof(CurrentAudioTime));
                if (AudioPlayer is null)
                    return false;
                return AudioPlayer.IsPlaying && AudioPlayer.IsAvaliable;
            }, TimeSpan.FromMilliseconds(100),
            DispatcherPriority.Background);
    }

    [RelayCommand]
    private void Pause()
    {
        AudioPlayer.Pause();

        timerDispose?.Dispose();
        timerDispose = default;
    }

    [RelayCommand]
    private void Stop()
    {
        AudioPlayer?.Stop();

        timerDispose?.Dispose();
        timerDispose = default;
    }

    [RelayCommand]
    private void JumpTo()
    {
        timerDispose?.Dispose();
        timerDispose = default;
    }

    [RelayCommand]
    private void CancelFullScreen()
    {
        windowManager.IsFullScreen = false;
    }

    [RelayCommand]
    private void ShowOrNotFullScreen()
    {
        windowManager.IsFullScreen = !windowManager.IsFullScreen;
    }

    [RelayCommand]
    private void ShowOrHideControlPanel(PointerEventArgs e)
    {
        IsControlPanelVisible = !IsControlPanelVisible;
        e.Handled = true;
    }

    [RelayCommand]
    private void PlayOrPause()
    {
        if (AudioPlayer is null)
            return;
        if (!AudioPlayer.IsAvaliable)
            return;

        if (AudioPlayer.IsPlaying)
            Pause();
        else
            Play();
    }

    [RelayCommand]
    private void VolumeMuteOrNot()
    {
        if (AudioPlayer is null)
            return;
        if (!AudioPlayer.IsAvaliable)
            return;

        if (AudioPlayer.Volume <= 0)
            AudioPlayer.Volume = 1;
        else
            AudioPlayer.Volume = 0;
    }

    [RelayCommand]
    private void JumpToTime(string millisecondsStr)
    {
        if (!int.TryParse(millisecondsStr, out var time))
            return;
        AudioPlayer?.Seek(TimeSpan.FromMilliseconds(time), true);
        OnPropertyChanged(nameof(CurrentAudioTime));
    }

    [RelayCommand]
    private void TimelineProgressBarMove(PointerEventArgs e)
    {
        e.Handled = true;
    }

    [RelayCommand]
    private void TimelineProgressBarClick(PointerEventArgs e)
    {
        if ((e.Source as Control)?.FindAncestorOfType<ProgressBar>(true) is not Control control)
            return;
        if (AudioPlayer is null)
            return;
        var point = e.GetPosition(control);

        var jumpToTime = TimeSpan.FromMilliseconds(Math.Clamp(point.X / control.Bounds.Width, 0, 1) *
            (AudioPlayer.Duration + AudioPlayer.LeadIn).TotalMilliseconds - AudioPlayer.LeadIn.TotalMilliseconds);
        AudioPlayer?.Seek(jumpToTime, true);
        logger.LogInformationEx(
            $"point: {point.X}, {point.Y} width: {control.Bounds.Width} inputJumpTime: {jumpToTime}, after: {AudioPlayer.CurrentTime} ");
        OnPropertyChanged(nameof(CurrentAudioTime));
        e.Handled = true;
    }
}
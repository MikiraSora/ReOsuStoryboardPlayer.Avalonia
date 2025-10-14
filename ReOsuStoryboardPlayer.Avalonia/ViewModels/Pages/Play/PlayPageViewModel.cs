using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Services.Dialog;
using ReOsuStoryboardPlayer.Avalonia.Services.Persistences;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Services.Window;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages.Play;

public partial class PlayPageViewModel : PageViewModelBase
{
    private readonly IAudioManager audioManager;
    private readonly IDialogManager dialogManager;
    private readonly ILogger<PlayPageViewModel> logger;
    private readonly IStoryboardLoader storyboardLoader;
    private readonly IWindowManager windowManager;

    [ObservableProperty]
    private IAudioPlayer audioPlayer;

    [ObservableProperty]
    private bool isControlPanelVisible = true;

    [ObservableProperty]
    private IStoryboardInstance storyboardInstance;

    [ObservableProperty]
    private float storyboardPlayTime;

    private IDisposable timerDispose;

    public PlayPageViewModel(
        IDialogManager dialogManager,
        IStoryboardLoader storyboardLoader,
        IPersistence persistence,
        ILogger<PlayPageViewModel> logger,
        IWindowManager windowManager,
        IAudioManager audioManager)
    {
        this.dialogManager = dialogManager;
        this.storyboardLoader = storyboardLoader;
        this.logger = logger;
        this.windowManager = windowManager;
        this.audioManager = audioManager;
    }

    public override string Title => "主页";

    public TimeSpan CurrentAudioTime => AudioPlayer?.CurrentTime ?? TimeSpan.Zero;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task LoadStoryboardInstance(CancellationToken token = default)
    {
        try
        {
            var instance = await storyboardLoader.OpenLoaderDialog();
            if (instance is null || token.IsCancellationRequested)
                return;

            Stop();
            AudioPlayer = await audioManager.LoadAudio(instance);
            StoryboardInstance = instance;
            StoryboardPlayTime = 0;
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
    private void JumpToTime(double milliseconds)
    {
        AudioPlayer?.Seek(TimeSpan.FromMilliseconds(milliseconds), true);
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
        logger.LogInformationEx($"point: {point.X}, {point.Y} width: {control.Bounds.Width} ");

        var jumpToTime = Math.Clamp(point.X / control.Bounds.Width, 0, 1) *
                         AudioPlayer.Duration.TotalMilliseconds;
        JumpToTime(jumpToTime);
        e.Handled = true;
    }
}
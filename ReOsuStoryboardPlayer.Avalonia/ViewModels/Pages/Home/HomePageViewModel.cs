using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Models;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Services.Dialog;
using ReOsuStoryboardPlayer.Avalonia.Services.Navigation;
using ReOsuStoryboardPlayer.Avalonia.Services.Persistences;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages.Play;

namespace ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages.Home;

public partial class HomePageViewModel : PageViewModelBase
{
    private readonly IAudioManager audioManager;
    private readonly IDialogManager dialogManager;
    private readonly ILogger<HomePageViewModel> logger;
    private readonly IPageNavigationManager pageNavigationManager;
    private readonly IPersistence persistence;
    private readonly IStoryboardLoader storyboardLoader;

    [ObservableProperty]
    private StoryboardPlayerSetting storyboardPlayerSetting;

    public HomePageViewModel(
        IDialogManager dialogManager,
        IStoryboardLoader storyboardLoader,
        IPersistence persistence,
        IPageNavigationManager pageNavigationManager,
        IAudioManager audioManager,
        ILogger<HomePageViewModel> logger)
    {
        this.dialogManager = dialogManager;
        this.storyboardLoader = storyboardLoader;
        this.persistence = persistence;
        this.pageNavigationManager = pageNavigationManager;
        this.audioManager = audioManager;
        this.logger = logger;

        Initaliaze();
    }

    public override string Title => "主页";

    private async void Initaliaze()
    {
        StoryboardPlayerSetting = await persistence.Load<StoryboardPlayerSetting>();
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task LoadStoryboardInstance(CancellationToken token = default)
    {
        try
        {
            var instance = await storyboardLoader.OpenLoaderDialog();
            if (instance is null || token.IsCancellationRequested)
                return;

            var audio = await audioManager.LoadAudio(instance);

            //switch to play page and never back~
            var playViewModel = await pageNavigationManager.SetPage<PlayPageViewModel>();
            playViewModel.StoryboardInstance = instance;
            playViewModel.StoryboardPlayTime = 0;
            playViewModel.AudioPlayer = audio;
        }
        catch (Exception e)
        {
            var msg = e.Message;
            logger.LogErrorEx(e, $"open storyboard dialog exception: {e.Message}");
            await dialogManager.ShowMessageDialog(msg, DialogMessageType.Error);
        }
    }

/*
    [RelayCommand]
    private async Task SayHello(CancellationToken token = default)
    {
        await dialogManager.ShowMessageDialog("Hello");
    }

    [RelayCommand]
    private async Task LoadAudio(CancellationToken token = default)
    {
        logger.LogInformationEx("PlayAudio() begin");
        var assetUri = new Uri("avares://ReOsuStoryboardPlayer.Avalonia/Assets/test.mp3");
        using var stream = AssetLoader.Open(assetUri);
        logger.LogInformationEx($"Playing audio: {assetUri}, stream: {stream}");
        audioPlayer = await audioManager.LoadAudio(stream);
    }

    [RelayCommand]
    private async Task PlayAudio(CancellationToken token = default)
    {
        audioPlayer?.Play();
    }

    [RelayCommand]
    private async Task PauseAudio(CancellationToken token = default)
    {
        audioPlayer?.Pause();
    }

    [RelayCommand]
    private async Task VolumeAudio(CancellationToken token = default)
    {
        audioPlayer.Volume = 0.25f;
    }

    [RelayCommand]
    private async Task SeekAudio(CancellationToken token = default)
    {
        audioPlayer?.Seek(TimeSpan.FromMilliseconds(160_000), true);
    }

    [RelayCommand]
    private async Task PrintAudioCurrentTime(CancellationToken token = default)
    {
        logger.LogInformationEx($"currentAudioTime: {audioPlayer?.CurrentTime}");
    }

    [RelayCommand]
    private async Task StopAudio(CancellationToken token = default)
    {
        AudioPlayer?.Stop();
    }
*/
}
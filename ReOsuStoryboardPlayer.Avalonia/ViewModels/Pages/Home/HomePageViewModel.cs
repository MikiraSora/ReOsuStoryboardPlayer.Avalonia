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
using ReOsuStoryboardPlayer.Avalonia.Services.Plaform;
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
    private readonly IPlatform platform;
    private readonly IStoryboardLoader storyboardLoader;

    [ObservableProperty]
    private StoryboardPlayerSetting storyboardPlayerSetting;

    public HomePageViewModel(
        IDialogManager dialogManager,
        IStoryboardLoader storyboardLoader,
        IPersistence persistence,
        IPageNavigationManager pageNavigationManager,
        IAudioManager audioManager,
        IPlatform platform,
        ILogger<HomePageViewModel> logger)
    {
        this.dialogManager = dialogManager;
        this.storyboardLoader = storyboardLoader;
        this.persistence = persistence;
        this.pageNavigationManager = pageNavigationManager;
        this.audioManager = audioManager;
        this.platform = platform;
        this.logger = logger;

        Initaliaze();
    }

    public WideScreenOption[] WideScreenOptions { get; } = Enum.GetValues<WideScreenOption>();

    public bool IsSupportMultiThreaded => platform.SupportMultiThread;

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
    
    [RelayCommand]
    private async Task SaveSetting(CancellationToken token = default)
    {
        await persistence.Save(StoryboardPlayerSetting);
        await dialogManager.ShowMessageDialog("Save success!");
    }
}
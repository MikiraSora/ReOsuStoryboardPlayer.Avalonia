using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Models;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Services.Dialog;
using ReOsuStoryboardPlayer.Avalonia.Services.Navigation;
using ReOsuStoryboardPlayer.Avalonia.Services.Parameters;
using ReOsuStoryboardPlayer.Avalonia.Services.Persistences;
using ReOsuStoryboardPlayer.Avalonia.Services.Plaform;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Services.Window;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem.Impl.Zip;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Dialogs.OpenStoryboard;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages.Play;
using SkiaSharp;

namespace ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages.Home;

public partial class HomePageViewModel : PageViewModelBase
{
    private readonly IAudioManager audioManager;
    private readonly IDialogManager dialogManager;
    private readonly ILogger<HomePageViewModel> logger;
    private readonly IPageNavigationManager pageNavigationManager;
    private readonly IParameterManager parameterManager;
    private readonly IPersistence persistence;
    private readonly IPlatform platform;
    private readonly IStoryboardLoadDialog storyboardLoadDialog;
    private readonly StoryboardLoader storyboardLoader;
    private readonly IWindowManager windowManager;

    [ObservableProperty]
    private StoryboardPlayerSetting storyboardPlayerSetting;

    public HomePageViewModel(
        IDialogManager dialogManager,
        IStoryboardLoadDialog storyboardLoadDialog,
        StoryboardLoader storyboardLoader,
        IPersistence persistence,
        IPageNavigationManager pageNavigationManager,
        IAudioManager audioManager,
        IParameterManager parameterManager,
        IPlatform platform,
        IWindowManager windowManager,
        ILogger<HomePageViewModel> logger)
    {
        this.dialogManager = dialogManager;
        this.storyboardLoadDialog = storyboardLoadDialog;
        this.storyboardLoader = storyboardLoader;
        this.persistence = persistence;
        this.pageNavigationManager = pageNavigationManager;
        this.audioManager = audioManager;
        this.parameterManager = parameterManager;
        this.platform = platform;
        this.windowManager = windowManager;
        this.logger = logger;

        Initaliaze();
    }

    public string ProgramCommitId => ThisAssembly.GitCommitId;
    public string ProgramCommitIdShort => ProgramCommitId[..7];
    public string AssemblyVersion => ThisAssembly.AssemblyVersion;
    public DateTime ProgramCommitDate => ThisAssembly.GitCommitDate + TimeSpan.FromHours(8);
    public string ProgramBuildConfiguration => ThisAssembly.AssemblyConfiguration;

    public string ProgramBuildTime
    {
        get
        {
            var type = typeof(HomePageViewModel).Assembly.GetType("DpMapSubscribeTool.BuildTime");
            var prop = type?.GetField("Value")?.GetValue(null)
                ?.ToString();
            return prop;
        }
    }

    public WideScreenOption[] WideScreenOptions { get; } = Enum.GetValues<WideScreenOption>();
    public SKFilterQuality[] FilterQualities { get; } = Enum.GetValues<SKFilterQuality>();
    public StoryboardPlayerSetting.WASAPIPeriod[] WASAPIPeriods { get; } = Enum.GetValues<StoryboardPlayerSetting.WASAPIPeriod>();

    public bool IsSupportMultiThreaded => platform.SupportMultiThread;

    public override string Title => "主页";

    private async void Initaliaze()
    {
        StoryboardPlayerSetting = await persistence.Load(StoryboardPlayerSetting.JsonTypeInfo);

        if (parameterManager.Parameters.TryGetArg("loadBeatmapSetId", out var loadBeatmapSetId))
            if (!int.TryParse(loadBeatmapSetId, out var beatmapSetId))
                await dialogManager.ShowMessageDialog(
                    $"can't download/load storyboard because param 'loadBeatmapSetId' is invaild:{loadBeatmapSetId}",
                    DialogMessageType.Error);
            else
                await LoadStoryboardFromBeatmapSet(beatmapSetId);
    }

    private async Task LoadStoryboardFromBeatmapSet(int beatmapSetId)
    {
        try
        {
            using var loadingDialog = new LoadingDialogViewModel();
            dialogManager.ShowDialog(loadingDialog).NoWait();
            await loadingDialog.WaitForAttachedView();

            var downloadUrl = $"https://dl.sayobot.cn/beatmaps/download/full/{beatmapSetId}";
            logger.LogInformationEx($"downloadUrl: {downloadUrl}");

            using var httpClient = new HttpClient();
            var zipBytes = await httpClient.GetByteArrayAsync(downloadUrl);
            logger.LogInformationEx($"download zip file successfully, size: {zipBytes.Length}");

            var dir = await ZipFileSystemBuilder.LoadFromZipFileBytes(zipBytes);
            var instance = await storyboardLoader.LoadStoryboard(dir);

            await LoadStoryboardInstance(instance);
        }
        catch (Exception e)
        {
            var msg = e.Message;
            logger.LogErrorEx(e, $"load storyboard throw exception: {e.Message}");
            await dialogManager.ShowMessageDialog(msg, DialogMessageType.Error);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task LoadStoryboardInstance(CancellationToken token = default)
    {
        try
        {
            var instance = await storyboardLoadDialog.OpenLoaderDialog();
            if (instance is null || token.IsCancellationRequested)
                return;

            using var loadingDialog = new LoadingDialogViewModel();
            dialogManager.ShowDialog(loadingDialog).NoWait();
            await loadingDialog.WaitForAttachedView();

            await LoadStoryboardInstance(instance);
        }
        catch (Exception e)
        {
            var msg = e.Message;
            logger.LogErrorEx(e, $"open storyboard dialog throw exception: {e.Message}");
            await dialogManager.ShowMessageDialog(msg, DialogMessageType.Error);
        }
    }

    private async Task LoadStoryboardInstance(StoryboardInstance instance, CancellationToken token = default)
    {
        var audio = await audioManager.LoadAudio(instance);
        logger.LogInformationEx($"load storyboard instance successfully, audio: {audio}");

        //switch to play page and never back~
        var playViewModel = await pageNavigationManager.SetPage<PlayPageViewModel>();
        playViewModel.StoryboardInstance = instance;
        playViewModel.StoryboardPlayTime = 0;
        playViewModel.AudioPlayer = audio;
        logger.LogInformationEx($"setup PlayPageViewModel done: {instance}");
    }

    [RelayCommand]
    private void OpenURL(string url)
    {
        windowManager.OpenUrl(url);
    }

    [RelayCommand]
    private async Task SaveSetting(CancellationToken token = default)
    {
        await persistence.Save(StoryboardPlayerSetting, StoryboardPlayerSetting.JsonTypeInfo);
        await dialogManager.ShowMessageDialog("Save success!");
    }
}
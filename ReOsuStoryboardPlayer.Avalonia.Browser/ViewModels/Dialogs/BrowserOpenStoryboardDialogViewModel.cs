using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Browser.Models;
using ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Browser.Utils;
using ReOsuStoryboardPlayer.Avalonia.Services.Dialog;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Dialogs;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ViewModels.Dialogs;

public partial class BrowserOpenStoryboardDialogViewModel(
    IStoryboardLoadDialog iStoryboardLoadDialog,
    ILogger<BrowserOpenStoryboardDialogViewModel> logger,
    IDialogManager dialogManager) : DialogViewModelBase
{
    [ObservableProperty]
    private StoryboardInstance downloadInstance;

    [ObservableProperty]
    private string downloadUrl;

    [ObservableProperty]
    private StoryboardInstance folderLoadInstance;

    [ObservableProperty]
    private bool loadFromUrl;

    [ObservableProperty]
    private OpenStoryboardMethods openMethod;

    [ObservableProperty]
    private StoryboardInstance parseInstance;

    [ObservableProperty]
    private string parseUrl;

    [ObservableProperty]
    private StoryboardInstance selectedStoryboardInstance;

    [ObservableProperty]
    private StoryboardInstance zipLoadInstance;

    public override string DialogIdentifier => nameof(BrowserOpenStoryboardDialogViewModel);

    public override string Title => "Open storyboard from...";

    [RelayCommand]
    private async Task OpenZipFromLocalFileSystem(CancellationToken cancellationToken)
    {
        if (iStoryboardLoadDialog is not BrowserStoryboardLoadDialog browserStoryboardLoader)
        {
            logger.LogErrorEx($"current loader is not supported: {iStoryboardLoadDialog?.GetType()?.FullName}");
            return;
        }

        try
        {
            var zipFileBytes = await LocalFileSystemInterop.PickFile();
            if (cancellationToken.IsCancellationRequested)
                return;

            using var loadingDialog = new LoadingDialogViewModel();
            dialogManager.ShowDialog(loadingDialog).NoWait();
            await loadingDialog.WaitForAttachedView();

            var instance = await browserStoryboardLoader.OpenLoaderFromZipFileBytes(zipFileBytes);
            if (cancellationToken.IsCancellationRequested)
                return;

            ZipLoadInstance = instance;
        }
        catch (Exception e)
        {
            logger.LogErrorEx(e, $"can't load storyboard from local .zip:{e.Message}");
            await dialogManager.ShowMessageDialog($"Can't load storyboard from local .zip/.osz file: {e.Message}",
                DialogMessageType.Error);
        }
    }

    [RelayCommand]
    private void SwitchMethod(string methodName)
    {
        OpenMethod = Enum.TryParse<OpenStoryboardMethods>(methodName, true, out var d) ? d : OpenMethod;
        logger.LogInformationEx($"cuurent OpenMethod: {OpenMethod}");
    }

    [RelayCommand]
    private async Task OpenFolderFromLocalFileSystem(CancellationToken cancellationToken)
    {
        if (iStoryboardLoadDialog is not BrowserStoryboardLoadDialog browserStoryboardLoader)
        {
            logger.LogErrorEx($"current loader is not supported: {iStoryboardLoadDialog?.GetType()?.FullName}");
            return;
        }

        try
        {
            var folder = await LocalFileSystemInterop.PickDirectory();
            if (cancellationToken.IsCancellationRequested)
                return;

            using var loadingDialog = new LoadingDialogViewModel();
            dialogManager.ShowDialog(loadingDialog).NoWait();
            await loadingDialog.WaitForAttachedView();

            var instance = await browserStoryboardLoader.OpenLoaderFromLocalFileSystem(folder);
            if (cancellationToken.IsCancellationRequested)
                return;

            FolderLoadInstance = instance;
        }
        catch (Exception e)
        {
            logger.LogErrorEx(e, $"can't load storyboard from local folder:{e.Message}");
            await dialogManager.ShowMessageDialog($"Can't load storyboard from local folder: {e.Message}",
                DialogMessageType.Error);
        }
    }

    [RelayCommand]
    private async Task Comfirm(CancellationToken cancellationToken)
    {
        logger.LogInformationEx($"current open method: {OpenMethod}");

        if (OpenMethod == OpenStoryboardMethods.ParseUrl)
        {
            if (!await TryLoadFromParsingBeatmapUrl(ParseUrl))
            {
                await dialogManager.ShowMessageDialog($"can't load storyboard from parsing beatmap url:{ParseUrl}",
                    DialogMessageType.Error);
                return;
            }

            SelectedStoryboardInstance = ParseInstance;
        }
        else if (OpenMethod == OpenStoryboardMethods.DownloadZipFile)
        {
            if (!await LoadFromDownloadingZipUrl(DownloadUrl))
            {
                await dialogManager.ShowMessageDialog($"can't load storyboard from parsing beatmap url:{DownloadUrl}",
                    DialogMessageType.Error);
                return;
            }

            SelectedStoryboardInstance = DownloadInstance;
        }
        else if (OpenMethod == OpenStoryboardMethods.OpenLocalBeatmapFolder)
        {
            if (FolderLoadInstance is null)
            {
                await dialogManager.ShowMessageDialog("local beatmap folder hasn't select/load yet",
                    DialogMessageType.Error);
                return;
            }

            SelectedStoryboardInstance = FolderLoadInstance;
        }
        else if (OpenMethod == OpenStoryboardMethods.OpenLocalZipFile)
        {
            if (ZipLoadInstance is null)
            {
                await dialogManager.ShowMessageDialog("local .zip/.osz file hasn't select/load yet",
                    DialogMessageType.Error);
                return;
            }

            SelectedStoryboardInstance = ZipLoadInstance;
        }

        CloseDialog();
    }

    private async Task<byte[]> DownloadFile(string dlUrl)
    {
        using var httpClient = new HttpClient();
        var startTime = DateTime.Now;
        logger.LogInformationEx($"begin download url: {dlUrl}");
        var bytes = await httpClient.GetByteArrayAsync(dlUrl);
        var downloadTime = DateTime.Now;
        logger.LogInformationEx($"download done, cost time: {(downloadTime - startTime).TotalMilliseconds:F2}ms");
        return bytes;
    }

    private async Task<bool> LoadFromDownloadingZipUrl(string dlUrl)
    {
        using var loadingDialog = new LoadingDialogViewModel();
        dialogManager.ShowDialog(loadingDialog).NoWait();

        if (iStoryboardLoadDialog is not BrowserStoryboardLoadDialog browserStoryboardLoader)
        {
            logger.LogErrorEx($"current loader is not supported: {iStoryboardLoadDialog?.GetType()?.FullName}");
            return false;
        }

        try
        {
            var zipFileBytes = await DownloadFile(dlUrl);
            var instance = await browserStoryboardLoader.OpenLoaderFromZipFileBytes(zipFileBytes);

            DownloadInstance = instance;
            return true;
        }
        catch (Exception e)
        {
            logger.LogErrorEx(e, $"can't load storyboard from .zip/.osz download:{dlUrl} {e.Message}");
            return false;
        }
    }

    private async Task<bool> TryLoadFromParsingBeatmapUrl(string beatmapUrl)
    {
        using var loadingDialog = new LoadingDialogViewModel();
        dialogManager.ShowDialog(loadingDialog).NoWait();

        try
        {
            if (iStoryboardLoadDialog is not BrowserStoryboardLoadDialog browserStoryboardLoader)
            {
                logger.LogErrorEx($"current loader is not supported: {iStoryboardLoadDialog?.GetType()?.FullName}");
                return false;
            }

            var match = Regex.Match(beatmapUrl, @"beatmapsets/(\d+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                logger.LogErrorEx($"beatmapset can't be matched: {beatmapUrl}");
                return false;
            }

            var beatmapSetId = int.Parse(match.Groups[1].Value);
            var zipFileBytes = default(byte[]);
            try
            {
                var buildDownloadUrl = $"https://dl.sayobot.cn/beatmaps/download/full/${beatmapSetId}";
                zipFileBytes = await DownloadFile(buildDownloadUrl);
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e,
                    $"osu.sayobot.cn can't download beatmap directly, please try other ways:{beatmapUrl} {e.Message}");
                return false;
            }

            var instance = await browserStoryboardLoader.OpenLoaderFromZipFileBytes(zipFileBytes);

            ParseInstance = instance;
            return true;
        }
        catch (Exception e)
        {
            logger.LogErrorEx(e, $"can't load storyboard from parsing url:{beatmapUrl} {e.Message}");
            return false;
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseDialog();
    }
}
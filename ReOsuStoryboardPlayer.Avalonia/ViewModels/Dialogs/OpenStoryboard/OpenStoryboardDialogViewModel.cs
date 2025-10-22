using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Models;
using ReOsuStoryboardPlayer.Avalonia.Services.Dialog;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem.Impl.AvaloniaStorageProvider;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem.Impl.Zip;

namespace ReOsuStoryboardPlayer.Avalonia.ViewModels.Dialogs.OpenStoryboard;

public partial class OpenStoryboardDialogViewModel(
    ILogger<OpenStoryboardDialogViewModel> logger,
    StoryboardLoader storyboardLoader,
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

    public override string DialogIdentifier => nameof(OpenStoryboardDialogViewModel);

    public override string Title => "Open storyboard from...";

    [RelayCommand]
    private async Task OpenZipFromLocalFileSystem(CancellationToken cancellationToken)
    {
        var topLevel = (Application.Current as App)?.TopLevel;
        if (topLevel is null)
        {
            logger.LogErrorEx("TopLevel is null");
            await dialogManager.ShowMessageDialog("Can't open file dialog because program internal error",
                DialogMessageType.Error);
            return;
        }

        var storageProvider = topLevel.StorageProvider;

        try
        {
            using var file = (await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select .zip/.osz file",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("zip file") {Patterns = ["*.zip"]},
                    new FilePickerFileType("beatmap file") {Patterns = ["*.osz"]}
                ]
            })).FirstOrDefault();

            using var loadingDialog = new LoadingDialogViewModel();
            dialogManager.ShowDialog(loadingDialog).NoWait();
            await loadingDialog.WaitForAttachedView();

            if (file is null)
            {
                logger.LogErrorEx("OpenFilePickerAsync() return null");
                return;
            }

            using var fs = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await fs.CopyToAsync(ms);
            if (cancellationToken.IsCancellationRequested)
                return;

            var dir = await ZipFileSystemBuilder.LoadFromZipFileBytes(ms.ToArray());
            if (cancellationToken.IsCancellationRequested)
                return;

            var instance = await storyboardLoader.LoadStoryboard(dir);
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
        var topLevel = (Application.Current as App)?.TopLevel;
        if (topLevel is null)
        {
            logger.LogErrorEx("TopLevel is null");
            await dialogManager.ShowMessageDialog("Can't open file dialog because program internal error",
                DialogMessageType.Error);
            return;
        }

        var storageProvider = topLevel.StorageProvider;

        try
        {
            var folder = (await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Select beatmap folder"
            })).FirstOrDefault();
            if (cancellationToken.IsCancellationRequested)
                return;

            using var loadingDialog = new LoadingDialogViewModel();
            dialogManager.ShowDialog(loadingDialog).NoWait();
            await loadingDialog.WaitForAttachedView();

            var dir = await AvaloniaStorageProviderFileSystemBuilder.LoadFromAvaloniaStorageFolder(folder);
            if (cancellationToken.IsCancellationRequested)
                return;

            var instance = await storyboardLoader.LoadStoryboard(dir);
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
        try
        {
            using var loadingDialog = new LoadingDialogViewModel();
            dialogManager.ShowDialog(loadingDialog).NoWait();
            await loadingDialog.WaitForAttachedView();
            
            var zipFileBytes = await DownloadFile(dlUrl);
            var dir = await ZipFileSystemBuilder.LoadFromZipFileBytes(zipFileBytes);
            var instance = await storyboardLoader.LoadStoryboard(dir);

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
        try
        {
            var match = Regex.Match(beatmapUrl, @"beatmapsets/(\d+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                logger.LogErrorEx($"beatmapset can't be matched: {beatmapUrl}");
                return false;
            }
            
            using var loadingDialog = new LoadingDialogViewModel();
            dialogManager.ShowDialog(loadingDialog).NoWait();
            await loadingDialog.WaitForAttachedView();

            var beatmapSetId = int.Parse(match.Groups[1].Value);
            byte[] zipFileBytes;
            try
            {
                var buildDownloadUrl = $"https://dl.sayobot.cn/beatmaps/download/full/${beatmapSetId}";
                zipFileBytes = await DownloadFile(buildDownloadUrl);
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e,
                    $"sayobot can't download beatmap directly, please try other ways:{beatmapUrl} {e.Message}");
                await dialogManager.ShowMessageDialog("sayobot can't download beatmap directly, please try other ways",
                    DialogMessageType.Error);
                return false;
            }

            var dir = await ZipFileSystemBuilder.LoadFromZipFileBytes(zipFileBytes);
            var instance = await storyboardLoader.LoadStoryboard(dir);

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
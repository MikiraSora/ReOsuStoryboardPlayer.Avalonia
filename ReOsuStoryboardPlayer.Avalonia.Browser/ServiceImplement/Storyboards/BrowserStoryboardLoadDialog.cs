using System.Threading.Tasks;
using Injectio.Attributes;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards.FileSystem;
using ReOsuStoryboardPlayer.Avalonia.Browser.Utils;
using ReOsuStoryboardPlayer.Avalonia.Browser.ViewModels.Dialogs;
using ReOsuStoryboardPlayer.Avalonia.Services.Dialog;
using ReOsuStoryboardPlayer.Avalonia.Services.Parameters;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem.Impl.Zip;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards;

[RegisterSingleton<IStoryboardLoadDialog>]
public class BrowserStoryboardLoadDialog(
    ILogger<BrowserStoryboardLoadDialog> logger,
    IDialogManager dialogManager,
    StoryboardLoader storyboardLoader,
    IParameterManager parameterManager)
    : IStoryboardLoadDialog
{
    private readonly IParameterManager parameterManager = parameterManager;

    public async ValueTask<StoryboardInstance> OpenLoaderDialog()
    {
        var openDialog = await dialogManager.ShowDialog<BrowserOpenStoryboardDialogViewModel>();
        return openDialog.SelectedStoryboardInstance;
    }

    public async ValueTask<StoryboardInstance> OpenLoaderFromZipFileBytes(byte[] zipFileBytes)
    {
        var fsRoot = ZipFileSystemBuilder.LoadFromZipFileBytes(zipFileBytes);
        return await LoadStoryboardInstance(fsRoot);
    }

    public async ValueTask<StoryboardInstance> OpenLoaderFromLocalFileSystem(
        LocalFileSystemInterop.JSDirectory jsDirRoot)
    {
        var fsRoot = BrowserFileSystemBuilder.LoadFromLocalFileSystem(jsDirRoot);
        return await LoadStoryboardInstance(fsRoot);
    }

    private async ValueTask<StoryboardInstance> LoadStoryboardInstance(ISimpleDirectory fsRoot)
    {
        return await storyboardLoader.LoadStoryboard(fsRoot);
    }
}
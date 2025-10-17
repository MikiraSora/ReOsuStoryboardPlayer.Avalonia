using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Injectio.Attributes;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Services.Parameters;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem.Impl.AvaloniaStorageProvider;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Storyboards;

[RegisterSingleton<IStoryboardLoadDialog>]
public class DesktopStoryboardLoadDialog(
    ILogger<DesktopStoryboardLoadDialog> logger,
    IParameterManager parameterManager,
    StoryboardLoader storyboardLoader)
    : IStoryboardLoadDialog
{
    private readonly IParameterManager parameterManager = parameterManager;

    public async ValueTask<StoryboardInstance> OpenLoaderDialog()
    {
        var toplevel = (App.Current as App).TopLevel;
        if (toplevel is null)
        {
            logger.LogErrorEx("top level is null");
            return default;
        }

        var folder = (await toplevel.StorageProvider?.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Select Beatmap Folder"
        })).FirstOrDefault();
        if (folder is null)
        {
            logger.LogErrorEx("OpenFolderPickerAsync() return null");
            return default;
        }

        var dir = await AvaloniaStorageProviderFileSystemBuilder.LoadFromAvaloniaStorageFolder(folder);

        return await storyboardLoader.LoadStoryboard(dir);
    }
}
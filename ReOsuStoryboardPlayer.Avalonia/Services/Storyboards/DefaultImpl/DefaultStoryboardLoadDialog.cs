using System.Threading.Tasks;
using Injectio.Attributes;
using ReOsuStoryboardPlayer.Avalonia.Services.Dialog;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Dialogs.OpenStoryboard;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Storyboards.DefaultImpl;

[RegisterSingleton<IStoryboardLoadDialog>]
public class DefaultStoryboardLoadDialog(
    IDialogManager dialogManager)
    : IStoryboardLoadDialog
{
    public async ValueTask<StoryboardInstance> OpenLoaderDialog()
    {
        var openDialog = await dialogManager.ShowDialog<OpenStoryboardDialogViewModel>();
        return openDialog.SelectedStoryboardInstance;
    }
}
using System.Threading.Tasks;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Dialogs;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Dialog;

public interface IDialogManager
{
    Task<T> ShowDialog<T>() where T : DialogViewModelBase;
    Task ShowDialog(DialogViewModelBase dialogViewModel);
    Task ShowMessageDialog(string content, DialogMessageType messageType = DialogMessageType.Info);
    Task<bool> ShowComfirmDialog(string content, string yesButtonContent = "确认", string noButtonContent = "取消");
}
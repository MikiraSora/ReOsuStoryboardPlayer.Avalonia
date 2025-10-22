using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReOsuStoryboardPlayer.Avalonia.Services.Dialog;
using ReOsuStoryboardPlayer.Avalonia.Utils;

namespace ReOsuStoryboardPlayer.Avalonia.ViewModels.Dialogs.CommonMessage;

public partial class CommonMessageDialogViewModel : DialogViewModelBase
{
    [ObservableProperty]
    private string content;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private DialogMessageType dialogMessageType;

    public CommonMessageDialogViewModel()
    {
        DesignModeHelper.CheckOnlyForDesignMode();
    }

    public CommonMessageDialogViewModel(DialogMessageType dialogMessageType, string content)
    {
        this.dialogMessageType = dialogMessageType;
        this.content = content;
    }

    public override string DialogIdentifier => nameof(CommonMessageDialogViewModel);

    public override string Title => DialogMessageType switch
    {
        DialogMessageType.Error => "Error",
        DialogMessageType.Info or _ => "Info"
    };

    [RelayCommand]
    private void Close()
    {
        CloseDialog();
    }
}
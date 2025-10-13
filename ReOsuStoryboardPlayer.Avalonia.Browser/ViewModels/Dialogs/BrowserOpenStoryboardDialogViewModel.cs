using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Dialogs;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ViewModels.Dialogs;

public partial class BrowserOpenStoryboardDialogViewModel : DialogViewModelBase
{
    [ObservableProperty]
    private bool loadFromUrl;

    [ObservableProperty]
    private string url;
    
    [ObservableProperty]
    private string diskPath;

    public override string DialogIdentifier => nameof(BrowserOpenStoryboardDialogViewModel);

    public override string Title => "Open storyboard from...";

    [RelayCommand]
    private void OpenFromUserDisk()
    {
    }

    [RelayCommand]
    private void Comfirm()
    {
        
        
        Close();
    }
    
    [RelayCommand]
    private void Close()
    {
        CloseDialog();
    }
}
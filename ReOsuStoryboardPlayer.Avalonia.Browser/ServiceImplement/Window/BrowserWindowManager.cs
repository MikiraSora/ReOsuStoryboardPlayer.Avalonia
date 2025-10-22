using CommunityToolkit.Mvvm.ComponentModel;
using Injectio.Attributes;
using ReOsuStoryboardPlayer.Avalonia.Services.Window;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Window;

[RegisterSingleton<IWindowManager>]
public partial class BrowserWindowManager : ObservableObject, IWindowManager
{
    [ObservableProperty]
    private string mainWindowTitle = "ReOsuStoryboardPlayer.Avalonia for Browser";

    public bool IsFullScreen
    {
        get => WindowInterop.IsFullScreen();
        set
        {
            if (value)
                WindowInterop.RequestFullScreen();
            else
                WindowInterop.ExitFullScreen();
            OnPropertyChanged();
        }
    }

    public void OpenUrl(string url)
    {
        WindowInterop.OpenURL(url);
    }
}
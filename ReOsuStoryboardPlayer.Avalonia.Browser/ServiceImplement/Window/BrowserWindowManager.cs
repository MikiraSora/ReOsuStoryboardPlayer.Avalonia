using CommunityToolkit.Mvvm.ComponentModel;
using ReOsuStoryboardPlayer.Avalonia.Services.Window;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Window;

[RegisterInjectable(typeof(IWindowManager))]
public class BrowserWindowManager : ObservableObject, IWindowManager
{
    public bool IsFullScreen
    {
        get => FullScreenInterop.IsFullScreen();
        set
        {
            if (value)
                FullScreenInterop.RequestFullScreen();
            else
                FullScreenInterop.ExitFullScreen();
            OnPropertyChanged();
        }
    }
}
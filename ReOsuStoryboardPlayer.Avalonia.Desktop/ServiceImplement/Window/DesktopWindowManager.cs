using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using ReOsuStoryboardPlayer.Avalonia.Services.Window;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Window;

[RegisterInjectable(typeof(IWindowManager))]
public partial class DesktopWindowManager : ObservableObject, IWindowManager
{
    [ObservableProperty]
    private bool isFullScreen;

    private WindowState prevWindowState = WindowState.Normal;

    public DesktopWindowManager()
    {
        if (GetMainWindow() is global::Avalonia.Controls.Window mainWindow)
            prevWindowState = mainWindow.WindowState;
    }

    partial void OnIsFullScreenChanged(bool oldValue, bool newValue)
    {
        if (newValue != oldValue)
            ApplyFullScreen(newValue);
    }

    public global::Avalonia.Controls.Window GetMainWindow()
    {
        if (App.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktopLifetime)
            return default;

        return desktopLifetime.Windows?.FirstOrDefault();
    }

    private void ApplyFullScreen(bool isFullScreen)
    {
        if (GetMainWindow() is not global::Avalonia.Controls.Window mainWindow)
            return;

        if (isFullScreen)
        {
            prevWindowState = mainWindow.WindowState;
            mainWindow.WindowState = WindowState.FullScreen;
        }
        else
        {
            mainWindow.WindowState = prevWindowState;
        }
    }
}
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using Injectio.Attributes;
using ReOsuStoryboardPlayer.Avalonia.Services.Window;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Window;

[RegisterSingleton<IWindowManager>]
public partial class DesktopWindowManager : ObservableObject, IWindowManager
{
    [ObservableProperty]
    private string mainWindowTitle = "ReOsuStoryboardPlayer.Avalonia for Windows";

    private WindowState prevWindowState = WindowState.Normal;

    public DesktopWindowManager()
    {
        if (GetMainWindow() is global::Avalonia.Controls.Window mainWindow)
            prevWindowState = mainWindow.WindowState;
    }

    public bool IsFullScreen
    {
        get => GetMainWindow()?.WindowState == WindowState.FullScreen;
        set
        {
            ApplyFullScreen(value);
            OnPropertyChanged();
        }
    }

    public void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    public global::Avalonia.Controls.Window GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktopLifetime)
            return default;

        return desktopLifetime.Windows.FirstOrDefault();
    }

    private void ApplyFullScreen(bool enable)
    {
        if (GetMainWindow() is not global::Avalonia.Controls.Window mainWindow)
            return;

        if (enable)
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
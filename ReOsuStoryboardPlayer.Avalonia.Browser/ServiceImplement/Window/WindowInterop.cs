using System.Runtime.InteropServices.JavaScript;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Window;

public partial class WindowInterop
{
    [JSImport("globalThis.WindowInterop.requestFullScreen")]
    public static partial void RequestFullScreen();

    [JSImport("globalThis.WindowInterop.exitFullScreen")]
    public static partial void ExitFullScreen();

    [JSImport("globalThis.WindowInterop.isFullScreen")]
    public static partial bool IsFullScreen();

    [JSImport("globalThis.WindowInterop.openURL")]
    public static partial bool OpenURL(string url);
}
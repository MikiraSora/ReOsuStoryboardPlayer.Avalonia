using System.Runtime.InteropServices.JavaScript;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Window;

public partial class FullScreenInterop
{
    [JSImport("globalThis.FullScreenInterop.requestFullScreen")]
    public static partial void RequestFullScreen();
    [JSImport("globalThis.FullScreenInterop.exitFullScreen")]
    public static partial void ExitFullScreen();
    [JSImport("globalThis.FullScreenInterop.isFullScreen")]
    public static partial bool IsFullScreen();
}
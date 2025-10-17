using System.Runtime.InteropServices.JavaScript;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.Utils;

public partial class MiscInterop
{
    [JSImport("globalThis.MiscInterop.getHref")]
    internal static partial string GetHref();
}
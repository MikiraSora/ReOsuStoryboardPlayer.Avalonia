using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Persistences;

public partial class LocalStorageInterop
{
    [JSImport("globalThis.LocalStorageInterop.load")]
    public static partial Task<string> Load([JSMarshalAs<JSType.String>] string key);

    [JSImport("globalThis.LocalStorageInterop.save")]
    public static partial Task Save([JSMarshalAs<JSType.String>] string key, [JSMarshalAs<JSType.String>] string value);
}
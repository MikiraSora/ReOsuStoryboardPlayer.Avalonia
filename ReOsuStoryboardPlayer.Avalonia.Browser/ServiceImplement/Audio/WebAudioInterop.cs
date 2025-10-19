using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using ReOsuStoryboardPlayer.Avalonia.Browser.Utils;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Audio;

public partial class WebAudioInterop
{
    [JSImport("globalThis.WebAudioInterop.hello")]
    public static partial void Hello();

    [JSImport("globalThis.WebAudioInterop.createPlayer")]
    public static partial void CreatePlayer([JSMarshalAs<JSType.String>] string id);

    [JSImport("globalThis.WebAudioInterop.loadFromBase64")]
    public static partial Task LoadFromBase64([JSMarshalAs<JSType.String>] string id, string base64,
        double prependLeadInSeconds);

    [JSImport("globalThis.WebAudioInterop.play")]
    public static partial void Play([JSMarshalAs<JSType.String>] string id);

    [JSImport("globalThis.WebAudioInterop.pause")]
    public static partial void Pause([JSMarshalAs<JSType.String>] string id);

    [JSImport("globalThis.WebAudioInterop.stop")]
    public static partial void Stop([JSMarshalAs<JSType.String>] string id);

    [JSImport("globalThis.WebAudioInterop.jumpToTime")]
    public static partial void JumpToTime([JSMarshalAs<JSType.String>] string id, double seconds,
        bool isPauseAfterJumped);

    [JSImport("globalThis.WebAudioInterop.getCurrentTime")]
    public static partial double GetCurrentTime([JSMarshalAs<JSType.String>] string id);

    [JSImport("globalThis.WebAudioInterop.getDuration")]
    public static partial double GetDuration([JSMarshalAs<JSType.String>] string id);

    [JSImport("globalThis.WebAudioInterop.dispose")]
    public static partial void Dispose([JSMarshalAs<JSType.String>] string id);

    [JSExport]
    public static void OnPlaybackEnded([JSMarshalAs<JSType.String>] string id)
    {
        JsConsoleLog.Log($"Send BrowserAudioPlayerPlaybackEndEvent:{id}");
        WeakReferenceMessenger.Default.Send(
            new BrowserAudioPlayerPlaybackEndEvent(id));
    }

    [JSImport("globalThis.WebAudioInterop.setVolume")]
    public static partial void SetVolume([JSMarshalAs<JSType.String>] string id, float volume);

    [JSImport("globalThis.WebAudioInterop.getVolume")]
    public static partial float GetVolume([JSMarshalAs<JSType.String>] string id);

    public record BrowserAudioPlayerPlaybackEndEvent(string BrowserAudioPlayerId);
}
import {dotnet} from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

const {getAssemblyExports} = await globalThis.getDotnetRuntime(0);
const exports = await getAssemblyExports(config.mainAssemblyName);

//import methods from .net/C#
if (!globalThis.AudioPlayer_OnPlaybackEnded) {
    var webaudio_OnPlaybackEnded = exports.ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Audio.WebAudioInterop.OnPlaybackEnded;
    globalThis.AudioPlayer_OnPlaybackEnded = webaudio_OnPlaybackEnded;
    console.log(`registered AudioPlayer_OnPlaybackEnded: ${webaudio_OnPlaybackEnded}`);
}

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);

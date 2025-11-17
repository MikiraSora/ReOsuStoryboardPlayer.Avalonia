using System;
using System.IO;
using System.Threading.Tasks;
using Injectio.Attributes;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Audio;

[RegisterSingleton<IAudioManager>]
public class BrowserAudioManager(ILogger<BrowserAudioManager> logger, IServiceProvider serviceProvider)
    : IAudioManager
{
    public async Task<IAudioPlayer> LoadAudio(Stream stream, double prependLeadInSeconds = 0)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        stream.Dispose();
        logger.LogInformationEx("ms copied");
        var audioPlayer = serviceProvider.Resolve<BrowserAudioPlayer>();
        logger.LogInformationEx("audioPlayer created");
        await audioPlayer.LoadFromAudioFileBytes(ms.ToArray(),prependLeadInSeconds);
        logger.LogInformationEx("audioPlayer loaded");
        return audioPlayer;
    }
}
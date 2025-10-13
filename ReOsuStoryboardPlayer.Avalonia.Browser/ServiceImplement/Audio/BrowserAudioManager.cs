using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards.FileSystem;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Audio;

[RegisterInjectable(typeof(IAudioManager), ServiceLifetime.Singleton)]
public class BrowserAudioManager(ILogger<BrowserAudioManager> logger, IServiceProvider serviceProvider)
    : IAudioManager
{
    public async Task<IAudioPlayer> LoadAudio(IStoryboardInstance storyboardInstance)
    {
        if (storyboardInstance is not BrowserStoryboardInstance instance)
            return default;

        var audioPath = instance.InfoEx.audio_file_path;
        using var fs = await BrowserSimpleIO.OpenRead(instance.StoryboardFileSystemRootDirectory, audioPath);
        return await LoadAudio(fs);
    }

    public async Task<IAudioPlayer> LoadAudio(Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        logger.LogInformationEx($"ms copied");
        var audioPlayer = serviceProvider.Resolve<BrowserAudioPlayer>();
        logger.LogInformationEx($"audioPlayer created");
        await audioPlayer.LoadFromAudioFileBytes(ms.ToArray());
        logger.LogInformationEx($"audioPlayer loaded");
        return audioPlayer;
    }
}
using System.IO;
using System.Threading.Tasks;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Audio;

public interface IAudioManager
{
    Task<IAudioPlayer> LoadAudio(Stream stream, double prependLeadInSeconds);

    async Task<IAudioPlayer> LoadAudio(ISimpleFile file, double prependLeadInSeconds)
    {
        return await LoadAudio(await file.OpenRead(), prependLeadInSeconds);
    }

    async Task<IAudioPlayer> LoadAudio(StoryboardInstance storyboardInstance)
    {
        var audioFilePath = storyboardInstance.Info.audio_file_path;
        var fs = await SimpleIO.OpenRead(storyboardInstance.FileSystemFolder, audioFilePath);
        return await LoadAudio(fs, storyboardInstance.Info.AudioLeadInSeconds);
    }
}
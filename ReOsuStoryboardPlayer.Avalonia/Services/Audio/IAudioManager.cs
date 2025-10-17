using System.IO;
using System.Threading.Tasks;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Audio;

public interface IAudioManager
{
    Task<IAudioPlayer> LoadAudio(Stream stream);

    async Task<IAudioPlayer> LoadAudio(ISimpleFile file)
    {
        using var fs = new MemoryStream(await file.ReadAllBytes());
        return await LoadAudio(fs);
    }

    async Task<IAudioPlayer> LoadAudio(StoryboardInstance storyboardInstance)
    {
        var audioFilePath = storyboardInstance.Info.audio_file_path;
        using var fs = await SimpleIO.OpenRead(storyboardInstance.FileSystemFolder, audioFilePath);
        return await LoadAudio(fs);
    }
}
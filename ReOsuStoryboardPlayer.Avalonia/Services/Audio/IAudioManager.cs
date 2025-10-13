using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Audio
{
    public interface IAudioManager
    {
        Task<IAudioPlayer> LoadAudio(Stream stream);
        Task<IAudioPlayer> LoadAudio(IStoryboardInstance storyboardInstance);
    }
}

using Injectio.Attributes;
using ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.AudioPlayer;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio
{
    [RegisterSingleton<IAudioManager>]
    internal class WinRTMediaPlayerManager : IAudioManager
    {
        public Task<IAudioPlayer> LoadAudio(Stream stream, double prependLeadInSeconds)
        {
            return Task.FromResult(new WinRTMediaPlayer(stream) as IAudioPlayer);
        }
    }
}

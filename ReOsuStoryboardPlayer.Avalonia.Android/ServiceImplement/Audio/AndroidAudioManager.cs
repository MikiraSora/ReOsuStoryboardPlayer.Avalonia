using Injectio.Attributes;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Android.ServiceImplement.Audio
{
    [RegisterSingleton<IAudioManager>]
    public class AndroidAudioManager : IAudioManager
    {
        private readonly IServiceProvider serviceProvider;

        public AndroidAudioManager(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task<IAudioPlayer> LoadAudio(Stream stream, double prependLeadInSeconds)
        {
            var player = serviceProvider.Resolve<AndroidAudioPlayer>();
            await player.Load(stream, prependLeadInSeconds);
            return player;
        }
    }
}

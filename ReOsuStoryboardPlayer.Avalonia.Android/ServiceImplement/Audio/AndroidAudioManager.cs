using Injectio.Attributes;
using ReOsuStoryboardPlayer.Avalonia.Models;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Services.Persistences;
using ReOsuStoryboardPlayer.Avalonia.Services.Plaform;
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
        private readonly IPersistence persistence;
        private readonly IPlatform platform;

        public AndroidAudioManager(IServiceProvider serviceProvider, IPersistence persistence, IPlatform platform)
        {
            this.serviceProvider = serviceProvider;
            this.persistence = persistence;
            this.platform = platform;
        }

        public async Task<IAudioPlayer> LoadAudio(Stream stream, double prependLeadInSeconds)
        {
            var playerSetting = await persistence.Load(StoryboardPlayerSetting.JsonTypeInfo);
            var player = serviceProvider.Resolve<AndroidAudioPlayer>();

            await Task.Run(() => player.Load(stream, prependLeadInSeconds, playerSetting.UpdateThreadCount, playerSetting.AudioSampleRate));
            return player;
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OngekiFumenEditor.Kernel.Audio.NAudioImpl;
using ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.AudioPlayer;
using ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio
{
    [RegisterInjectable(typeof(IAudioManager), ServiceLifetime.Singleton)]
    public partial class DesktopAudioManager : ObservableObject, IAudioManager
    {
        private HashSet<WeakReference<DesktopAudioPlayer>> ownAudioPlayerRefs = new();
        private int targetSampleRate = 48000;
        private readonly IWavePlayer audioOutputDevice;

        private readonly MixingSampleProvider masterMixer;

        private readonly ILogger<DesktopAudioManager> logger;
        private readonly ILogger<DesktopAudioPlayer> audioLogger;

        public IEnumerable<(string fileExt, string extDesc)> SupportAudioFileExtensionList { get; } = new[] {
            (".mp3","Audio File"),
            (".wav","Audio File"),
        };

        public DesktopAudioManager(ILogger<DesktopAudioManager> logger, ILogger<DesktopAudioPlayer> audioLogger)
        {
            var audioOutputType = AudioOutputType.Wasapi;

            try
            {
                audioOutputDevice = audioOutputType switch
                {
                    AudioOutputType.Asio => new AsioOut() { AutoStop = false },
                    AudioOutputType.Wasapi => new WasapiOut(AudioClientShareMode.Shared, 0),
                    //AudioOutputType.WaveOut or _ => new WaveOut() { DesiredLatency = 100 },
                };
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e, $"Can't create audio output device:{audioOutputType}");
                throw;
            }
            logger.LogDebugEx($"audioOutputDevice: {audioOutputDevice}");

            var format = WaveFormat.CreateIeeeFloatWaveFormat(targetSampleRate, 2);

            masterMixer = new MixingSampleProvider(format);
            masterMixer.ReadFully = true;
            audioOutputDevice.Init(masterMixer);
            audioOutputDevice.Play();

            logger.LogInformationEx($"Audio output will use {audioOutputType}");
            this.logger = logger;
            this.audioLogger = audioLogger;
        }

        public async Task<IAudioPlayer> LoadAudio(Stream stream)
        {
            if (stream is null)
                return null;

            var player = new DesktopAudioPlayer(masterMixer, audioLogger);
            ownAudioPlayerRefs.Add(new(player));
            await player.Load(stream, targetSampleRate);
            return player;
        }

        public void Dispose()
        {
            logger.LogDebugEx("call Dispose()");
            foreach (var weakRef in ownAudioPlayerRefs)
            {
                if (weakRef.TryGetTarget(out var player))
                    player?.Dispose();
            }
            ownAudioPlayerRefs.Clear();
            audioOutputDevice?.Dispose();
        }

        public Task<IAudioPlayer> LoadAudio(IStoryboardInstance storyboardInstance)
        {
            if ((storyboardInstance as DesktopStoryboardInstance)?.Info is not BeatmapFolderInfoEx info)
                return Task.FromResult<IAudioPlayer>(null);


            var audioPath = info.audio_file_path;
            var fs = File.OpenRead(audioPath);
            return LoadAudio(fs);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Injectio.Attributes;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OngekiFumenEditor.Kernel.Audio.NAudioImpl;
using ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.AudioPlayer;
using ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Models;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Services.Persistences;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio;

[RegisterSingleton<IAudioManager>]
public class DesktopAudioManager : ObservableObject, IAudioManager
{
    private readonly ILogger<DesktopAudioPlayer> audioLogger;

    private readonly ILogger<DesktopAudioManager> logger;
    private readonly HashSet<WeakReference<DesktopAudioPlayer>> ownAudioPlayerRefs = new();
    private readonly IPersistence persistence;
    private IWavePlayer audioOutputDevice;

    private MixingSampleProvider masterMixer;

    public DesktopAudioManager(ILogger<DesktopAudioManager> logger, ILogger<DesktopAudioPlayer> audioLogger,
        IPersistence persistence)
    {
        this.logger = logger;
        this.audioLogger = audioLogger;
        this.persistence = persistence;

        Initalize();
    }

    public IEnumerable<(string fileExt, string extDesc)> SupportAudioFileExtensionList { get; } = new[]
    {
        (".mp3", "Audio File"),
        (".wav", "Audio File")
    };

    public async Task<IAudioPlayer> LoadAudio(Stream stream)
    {
        if (stream is null)
            return null;

        var player = new DesktopAudioPlayer(masterMixer, audioLogger);
        ownAudioPlayerRefs.Add(new WeakReference<DesktopAudioPlayer>(player));

        var playerSetting = await persistence.Load<StoryboardPlayerSetting>(default);
        await player.Load(stream, playerSetting.AudioSampleRate);
        return player;
    }

    public Task<IAudioPlayer> LoadAudio(IStoryboardInstance storyboardInstance)
    {
        if ((storyboardInstance as DesktopStoryboardInstance)?.Info is not BeatmapFolderInfoEx info)
            return Task.FromResult<IAudioPlayer>(null);

        var audioPath = info.audio_file_path;
        var fs = File.OpenRead(audioPath);
        return LoadAudio(fs);
    }

    private async void Initalize()
    {
        var playerSetting = await persistence.Load<StoryboardPlayerSetting>(default);
        var audioOutputType = AudioOutputType.Wasapi;

        try
        {
            audioOutputDevice = audioOutputType switch
            {
                AudioOutputType.Asio => new AsioOut {AutoStop = false},
                _ => new WasapiOut(AudioClientShareMode.Shared, 0)
                //AudioOutputType.WaveOut or _ => new WaveOut() { DesiredLatency = 100 },
            };
        }
        catch (Exception e)
        {
            logger.LogErrorEx(e, $"Can't create audio output device:{audioOutputType}");
            throw;
        }

        logger.LogDebugEx($"audioOutputDevice: {audioOutputDevice}");

        var format = WaveFormat.CreateIeeeFloatWaveFormat(playerSetting.AudioSampleRate, 2);

        masterMixer = new MixingSampleProvider(format);
        masterMixer.ReadFully = true;
        audioOutputDevice.Init(masterMixer);
        audioOutputDevice.Play();

        logger.LogInformationEx($"Audio output will use {audioOutputType}");
    }

    public void Dispose()
    {
        logger.LogDebugEx("call Dispose()");
        foreach (var weakRef in ownAudioPlayerRefs)
            if (weakRef.TryGetTarget(out var player))
                player?.Dispose();
        ownAudioPlayerRefs.Clear();
        audioOutputDevice?.Dispose();
    }
}
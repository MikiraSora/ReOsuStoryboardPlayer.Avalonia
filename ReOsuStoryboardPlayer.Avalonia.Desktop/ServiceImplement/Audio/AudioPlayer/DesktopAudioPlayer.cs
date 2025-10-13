using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OngekiFumenEditor.Kernel.Audio.NAudioImpl.Utils;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.AudioPlayer;

public partial class DesktopAudioPlayer : ObservableObject, IAudioPlayer
{
    private readonly ILogger<DesktopAudioPlayer> logger;

    private readonly MixingSampleProvider mixer;
    private readonly Stopwatch sw = new();
    private BufferWaveStream audioFileReader;

    private TimeSpan baseOffset = TimeSpan.FromMilliseconds(0);

    [ObservableProperty]
    private TimeSpan duration;

    private FinishedListenerProvider finishProvider;

    [ObservableProperty]
    private bool isAvaliable;

    [ObservableProperty]
    private bool isPlaying;

    private VolumeSampleProvider musicVolumeWrapper;

    private TimeSpan pauseTime;

    public DesktopAudioPlayer(MixingSampleProvider mixer, ILogger<DesktopAudioPlayer> logger)
    {
        this.mixer = mixer;
        this.logger = logger;
    }

    public float Volume
    {
        get => musicVolumeWrapper?.Volume ?? 1;
        set
        {
            if (musicVolumeWrapper is not null)
                musicVolumeWrapper.Volume = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan CurrentTime => GetTime();

    public void Seek(TimeSpan seekTime, bool pause)
    {
        seekTime = TimeSpan.FromMilliseconds(Math.Max(0,
            Math.Min(seekTime.TotalMilliseconds, Duration.TotalMilliseconds)));

        audioFileReader.Seek((long) (audioFileReader.WaveFormat.AverageBytesPerSecond * seekTime.TotalSeconds),
            SeekOrigin.Begin);
        //more accurate
        baseOffset = audioFileReader.CurrentTime;
        pauseTime = pause ? seekTime : pauseTime;
        
        if (!pause)
            Play();
        else
            Pause();
    }

    public void Play()
    {
        if (!IsAvaliable)
            return;
        if (IsPlaying)
            return;

        baseOffset = GetTime();
        IsPlaying = true;
        sw.Restart();
        mixer.AddMixerInput(finishProvider);
        finishProvider.StartListen();
    }

    public void Stop()
    {
        if (!IsAvaliable)
            return;
        if (!IsPlaying)
            return;
        IsPlaying = false;
        baseOffset = pauseTime = TimeSpan.Zero;
        mixer.RemoveMixerInput(finishProvider);
        Seek(TimeSpan.FromMilliseconds(0), true);
    }

    public void Pause()
    {
        if (!IsAvaliable)
            return;
        if (!IsPlaying)
            return;
        pauseTime = GetTime();
        IsPlaying = false;
        mixer.RemoveMixerInput(finishProvider);
    }

    private void Provider_OnReturnEmptySamples()
    {
        finishProvider.StopListen();
        Pause();
    }

    public async Task Load(Stream stream, int targetSampleRate)
    {
        //release resource before loading new one.
        Dispose();

        try
        {
            var rawStream = new StreamMediaFoundationReader(stream);
            var processedProvider =
                await AudioCompatibilizer.CheckCompatible(rawStream.ToSampleProvider(), targetSampleRate);

            audioFileReader = new BufferWaveStream(processedProvider.ToWaveProvider().ToArray(),
                processedProvider.WaveFormat);
            audioFileReader.Seek(0, SeekOrigin.Begin);

            musicVolumeWrapper = new VolumeSampleProvider(audioFileReader);

            finishProvider = new FinishedListenerProvider(musicVolumeWrapper);
            finishProvider.StartListen();
            finishProvider.OnReturnEmptySamples += Provider_OnReturnEmptySamples;

            Duration = rawStream.TotalTime;
            IsAvaliable = true;
        }
        catch (Exception e)
        {
            logger.LogErrorEx(e, $"Load audio file failed : {e.Message}");
            Dispose();
        }
    }

    private TimeSpan GetTime()
    {
        if (!IsPlaying)
            return pauseTime;
        var offset = TimeSpan.FromTicks(sw.ElapsedTicks);
        var adjustedTime = offset + baseOffset;
        var actualTime = TimeSpan.FromMilliseconds(Math.Max(0, adjustedTime.TotalMilliseconds));
        return actualTime;
    }

    private void CleanCurrentOut()
    {
        mixer.RemoveMixerInput(finishProvider);
    }

    public void Dispose()
    {
        CleanCurrentOut();

        audioFileReader?.Dispose();
        audioFileReader = null;
        IsAvaliable = false;
        IsPlaying = false;
    }
}
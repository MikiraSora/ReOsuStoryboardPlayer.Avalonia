using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using NAudio.Vorbis;
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

    [ObservableProperty]
    private TimeSpan leadIn;

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

    public TimeSpan CurrentTime => GetTime() - LeadIn;

    public void Seek(TimeSpan seekTime, bool pause)
    {
        var actualSeekTime = TimeSpan.FromMilliseconds(Math.Max(0,
            Math.Min(seekTime.TotalMilliseconds + LeadIn.TotalMilliseconds,
                Duration.TotalMilliseconds + LeadIn.TotalMilliseconds)));

        audioFileReader.Seek((long) (audioFileReader.WaveFormat.AverageBytesPerSecond * actualSeekTime.TotalSeconds),
            SeekOrigin.Begin);
        //more accurate
        baseOffset = audioFileReader.CurrentTime;
        pauseTime = pause ? actualSeekTime : pauseTime;

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

    private Task<(ISampleProvider SampleProvider, TimeSpan duration)> TryLoadNormal(Stream stream)
    {
        try
        {
            var rawStream = new StreamMediaFoundationReader(stream);
            return Task.FromResult((rawStream.ToSampleProvider(), rawStream.TotalTime));
        }
        catch (Exception e)
        {
            logger.LogErrorEx(e, $"Load audio file failed : {e.Message}");
            return Task.FromResult<(ISampleProvider SampleProvider, TimeSpan duration)>(default);
        }
    }

    private Task<(ISampleProvider SampleProvider, TimeSpan duration)> TryLoadOgg(Stream stream)
    {
        try
        {
            var rawStream = new VorbisWaveReader(stream);
            return Task.FromResult((rawStream.ToSampleProvider(), rawStream.TotalTime));
        }
        catch (Exception e)
        {
            logger.LogErrorEx(e, $"Load audio file failed : {e.Message}");
            return Task.FromResult<(ISampleProvider SampleProvider, TimeSpan duration)>(default);
        }
    }

    public async Task Load(Stream stream, int targetSampleRate, double prependLeadInSeconds)
    {
        //release resource before loading new one.
        Dispose();

        try
        {
            ISampleProvider sampleProvider;
            TimeSpan totalTime;

            (sampleProvider, totalTime) = await TryLoadNormal(stream);
            if (sampleProvider is null)
                (sampleProvider, totalTime) = await TryLoadOgg(stream);

            if (sampleProvider is null)
                throw new Exception("audio file stream is unknown/notSupport format ");

            var processedProvider =
                await AudioCompatibilizer.CheckCompatible(sampleProvider, targetSampleRate);
            var silence = new SilenceProvider(processedProvider.WaveFormat)
                .ToSampleProvider()
                .Take(TimeSpan.FromSeconds(prependLeadInSeconds));
            var leadInProvider = new ConcatenatingSampleProvider([silence, processedProvider]);

            audioFileReader = new BufferWaveStream(leadInProvider.ToWaveProvider().ToArray(),
                processedProvider.WaveFormat);
            audioFileReader.Seek(0, SeekOrigin.Begin);

            musicVolumeWrapper = new VolumeSampleProvider(audioFileReader);

            finishProvider = new FinishedListenerProvider(musicVolumeWrapper);
            finishProvider.StartListen();
            finishProvider.OnReturnEmptySamples += Provider_OnReturnEmptySamples;

            LeadIn = TimeSpan.FromSeconds(prependLeadInSeconds);
            Duration = totalTime;
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
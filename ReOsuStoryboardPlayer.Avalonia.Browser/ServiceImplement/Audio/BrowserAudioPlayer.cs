using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Audio;

public partial class BrowserAudioPlayer : ObservableObject, IAudioPlayer
{
    private readonly string id;
    private readonly ILogger<BrowserAudioPlayer> logger;

    [ObservableProperty]
    private TimeSpan duration;

    [ObservableProperty]
    private bool isAvaliable;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private TimeSpan leadIn;

    public BrowserAudioPlayer(ILogger<BrowserAudioPlayer> logger)
    {
        this.logger = logger;
        id = Guid.NewGuid().ToString("N");
        logger.LogInformationEx($"BrowserAudioPlayer() id:{id}");
        WebAudioInterop.CreatePlayer(id);
        WeakReferenceMessenger.Default.Register<WebAudioInterop.BrowserAudioPlayerPlaybackEndEvent>(this,
            OnBrowserAudioPlayerPlaybackEnd);
    }

    public float Volume
    {
        get => WebAudioInterop.GetVolume(id);
        set
        {
            WebAudioInterop.SetVolume(id, value);
            OnPropertyChanged();
        }
    }

    public TimeSpan CurrentTime => TimeSpan.FromSeconds(GetCurrentPlayTime()) - LeadIn;

    public void Play()
    {
        if (!IsAvaliable)
            return;
        if (IsPlaying)
            return;
        IsPlaying = true;
        WebAudioInterop.Play(id);
        logger.LogInformationEx($"called by id:{id}");
    }

    public void Pause()
    {
        if (!IsAvaliable)
            return;
        if (!IsPlaying)
            return;
        IsPlaying = false;
        WebAudioInterop.Pause(id);
        logger.LogInformationEx($"called by id:{id}");
    }

    public void Stop()
    {
        if (!IsAvaliable)
            return;
        IsPlaying = false;
        WebAudioInterop.Stop(id);
        logger.LogInformationEx($"called by id:{id}");
    }

    public void Seek(TimeSpan timeSpan, bool pause)
    {
        if (!IsAvaliable)
            return;
        var actualSeekTime = timeSpan;
        logger.LogInformationEx($"called by id:{id}, actualSeekTime:{actualSeekTime}, pause:{pause}");
        WebAudioInterop.JumpToTime(id, actualSeekTime.TotalSeconds, pause);
        IsPlaying = !pause;
    }

    public void Dispose()
    {
        WebAudioInterop.Dispose(id);
        logger.LogInformationEx($"called by id:{id}");
    }

    private void OnBrowserAudioPlayerPlaybackEnd(object recipient,
        WebAudioInterop.BrowserAudioPlayerPlaybackEndEvent message)
    {
        if (message.BrowserAudioPlayerId != id)
            return;

        Stop();
        logger.LogInformationEx($"called by id:{id}");
    }

    public async Task LoadFromAudioFileBytes(byte[] audioData, double prependLeadInSeconds = 0)
    {
        var base64 = Convert.ToBase64String(audioData);
        logger.LogDebugEx($"audioData size: {audioData.Length}");
        await WebAudioInterop.LoadFromBase64(id, base64, prependLeadInSeconds);
        logger.LogInformationEx("LoadFromBase64() done");
        LeadIn = TimeSpan.FromSeconds(prependLeadInSeconds);
        Duration = TimeSpan.FromSeconds(GetDuration()) - LeadIn;
        logger.LogInformationEx($"Duration: {Duration}");
        logger.LogInformationEx($"called by id:{id}");

        IsAvaliable = true;
    }

    private double GetCurrentPlayTime()
    {
        return WebAudioInterop.GetCurrentTime(id);
    }

    private double GetDuration()
    {
        return WebAudioInterop.GetDuration(id);
    }
}
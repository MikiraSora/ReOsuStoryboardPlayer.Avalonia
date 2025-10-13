using System;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Audio;

public partial class BrowserAudioPlayer : ObservableObject, IAudioPlayer, IDisposable
{
    private readonly string id;
    private readonly ILogger<BrowserAudioPlayer> logger;

    [ObservableProperty]
    private TimeSpan duration;

    [ObservableProperty]
    private bool isAvaliable;

    [ObservableProperty]
    private bool isPlaying;

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

    public TimeSpan CurrentTime => TimeSpan.FromSeconds(GetCurrentPlayTime());

    public void Play()
    {
        if (!IsAvaliable)
            return;
        if (IsPlaying)
            return;
        WebAudioInterop.Play(id);
        IsPlaying = true;
        logger.LogInformationEx($"called by id:{id}");
    }

    public void Pause()
    {
        WebAudioInterop.Pause(id);
        IsPlaying = false;
        logger.LogInformationEx($"called by id:{id}");
    }

    public void Stop()
    {
        WebAudioInterop.Stop(id);
        IsPlaying = false;
        logger.LogInformationEx($"called by id:{id}");
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void Seek(TimeSpan timeSpan, bool pause)
    {
        WebAudioInterop.JumpToTime(id, timeSpan.TotalSeconds, pause);
        logger.LogInformationEx($"called by id:{id}");
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

    public async Task LoadFromAudioFileBytes(byte[] audioData)
    {
        var base64 = Convert.ToBase64String(audioData);
        logger.LogInformationEx($"base64: {base64}");
        await WebAudioInterop.LoadFromBase64(id, base64);
        logger.LogInformationEx("LoadFromBase64() done");
        Duration = TimeSpan.FromSeconds(GetDuration());
        logger.LogInformationEx($"Duration: {Duration}");
        logger.LogInformationEx($"called by id:{id}");
        IsAvaliable = true;
    }

    public double GetCurrentPlayTime()
    {
        return WebAudioInterop.GetCurrentTime(id);
    }

    public double GetDuration()
    {
        return WebAudioInterop.GetDuration(id);
    }
}
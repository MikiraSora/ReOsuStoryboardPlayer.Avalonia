using System;
using System.ComponentModel;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Audio;

public interface IAudioPlayer : INotifyPropertyChanged
{
    TimeSpan CurrentTime { get; }

    float Volume { get; set; }

    TimeSpan Duration { get; }

    bool IsPlaying { get; }

    bool IsAvaliable { get; }
    
    void Play();
    void Stop();
    void Pause();
    void Seek(TimeSpan TimeSpan, bool pause);
}
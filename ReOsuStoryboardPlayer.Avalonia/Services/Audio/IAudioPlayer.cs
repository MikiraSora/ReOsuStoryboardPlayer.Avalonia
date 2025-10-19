using System;
using System.ComponentModel;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Audio;

public interface IAudioPlayer : INotifyPropertyChanged
{
    /// <summary>
    ///     applied LeadIn
    /// </summary>
    TimeSpan CurrentTime { get; }

    float Volume { get; set; }

    /// <summary>
    ///     not include LeadIn
    /// </summary>
    TimeSpan Duration { get; }

    TimeSpan LeadIn { get; }

    bool IsPlaying { get; }

    bool IsAvaliable { get; }

    void Play();
    void Stop();
    void Pause();
    void Seek(TimeSpan TimeSpan, bool pause);
}
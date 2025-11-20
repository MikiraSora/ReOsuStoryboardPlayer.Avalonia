using CommunityToolkit.Mvvm.ComponentModel;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.AudioPlayer
{
    internal partial class WinRTMediaPlayer : ObservableObject, IAudioPlayer
    {
        [ObservableProperty]
        private float volume = 1.0f;
        [ObservableProperty]
        public bool isPlaying;
        [ObservableProperty]
        public bool isAvaliable;
        [ObservableProperty]
        private TimeSpan leadIn;

        private Windows.Media.Playback.MediaPlayer mediaPlayer;
        private bool disposedValue;
        private Stream stream;
        private IRandomAccessStream randomAccessStream;

        public WinRTMediaPlayer(Stream stream)
        {
            mediaPlayer = new();
            this.stream = stream;
            randomAccessStream = stream.AsRandomAccessStream();
            mediaPlayer.SetStreamSource(randomAccessStream);
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        }

        private void MediaPlayer_MediaFailed(Windows.Media.Playback.MediaPlayer sender, Windows.Media.Playback.MediaPlayerFailedEventArgs args)
        {
            throw args.ExtendedErrorCode;
        }

        private void MediaPlayer_MediaOpened(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            OnPropertyChanged(nameof(Duration));
            IsAvaliable = true;
        }

        private void MediaPlayer_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            IsPlaying = false;
        }

        public TimeSpan CurrentTime => mediaPlayer.PlaybackSession.Position;

        public TimeSpan Duration => mediaPlayer.PlaybackSession.NaturalDuration;

        public void Play()
        {
            mediaPlayer.Play();
            IsPlaying = true;
        }

        public void Stop()
        {
            mediaPlayer.Pause();
            mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
            IsPlaying = false;
        }

        public void Pause()
        {
            mediaPlayer.Pause();
            IsPlaying = false;
        }

        public void Seek(TimeSpan timeSpan, bool pause)
        {
            mediaPlayer.PlaybackSession.Position = timeSpan;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                mediaPlayer.Dispose();
                stream.Dispose();
                disposedValue = true;
            }
        }

        ~WinRTMediaPlayer()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

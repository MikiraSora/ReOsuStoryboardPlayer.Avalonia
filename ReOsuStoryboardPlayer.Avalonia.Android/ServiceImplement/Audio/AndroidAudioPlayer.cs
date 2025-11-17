using AcbGenerator;
using Android.Media;
using AndroidX.Core.Content;
using AndroidX.Media3.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using static Android.Icu.Text.Transliterator;

namespace ReOsuStoryboardPlayer.Avalonia.Android.ServiceImplement.Audio
{
    public partial class AndroidAudioPlayer : ObservableObject, IAudioPlayer
    {
        private MediaPlayer player;
        private TaskCompletionSource prepareTokenSource = new();

        private class Listener : Java.Lang.Object, MediaPlayer.IOnPreparedListener, MediaPlayer.IOnCompletionListener
        {
            private AndroidAudioPlayer parent;
            public Listener(AndroidAudioPlayer parent)
            {
                this.parent = parent;
            }
            public void OnCompletion(MediaPlayer mp)
            {
                parent.OnCompletion(mp);
            }
            public void OnPrepared(MediaPlayer mp)
            {
                parent.OnPrepared(mp);
            }
        }

        private async Task<string> WriteToRandomTempFile(System.IO.Stream stream, string ext)
        {
            if (!ext.StartsWith("."))
                ext = "." + ext;

            var context = global::Android.App.Application.Context;
            var cacheDir = context.CacheDir.AbsolutePath;

            var fileName = Path.GetRandomFileName() + ext;
            var filePath = Path.Combine(cacheDir, fileName);

            using (var fileStream = File.Create(filePath))
            {
                await stream.CopyToAsync(fileStream);
            }

            var authority = context.PackageName + ".fileprovider";
            var javaFile = new Java.IO.File(filePath);
            var uri = FileProvider.GetUriForFile(context, authority, javaFile);

            return uri.ToString();
        }

        public AndroidAudioPlayer(ILogger<AndroidAudioPlayer> logger, WavGenerator wavGenerator)
        {
            this.logger = logger;
            this.wavGenerator = wavGenerator;
        }

        public async Task Load(System.IO.Stream stream, double prependLeadInSeconds, int updateThreadCount, int audioSampleRate)
        {
            LeadIn = TimeSpan.FromSeconds(prependLeadInSeconds);
            player = new MediaPlayer();

            var output = wavGenerator.GenerateWavFileStream(stream, prependLeadInSeconds, audioSampleRate, updateThreadCount);

            // 将流保存到临时文件，因为 MediaPlayer 不支持直接读取 Stream
            var uri = await WriteToRandomTempFile(output, ".audioFile");
            logger.LogInformationEx($"temp file uri: {uri}");

            prepareTokenSource = new();
            listener = new Listener(this);

            player.SetDataSource(global::Android.App.Application.Context, global::Android.Net.Uri.Parse(uri));
            player.SetOnPreparedListener(listener);
            player.SetOnCompletionListener(listener);
            player.PrepareAsync();

            logger.LogInformationEx($"wait for AudioPlayer ready");
            await prepareTokenSource.Task;
        }

        public TimeSpan CurrentTime => GetPlayerCurrentTime();

        private TimeSpan GetPlayerCurrentTime()
        {
            return IsAvaliable
                        ? TimeSpan.FromMilliseconds(player.CurrentPosition)
                        : TimeSpan.Zero;
        }

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                player.SetVolume(value, value);
            }
        }
        private float _volume = 1.0f;

        [ObservableProperty]
        private TimeSpan duration;

        [ObservableProperty]
        private TimeSpan leadIn;

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private bool isAvaliable;
        private Listener listener;
        private readonly ILogger<AndroidAudioPlayer> logger;
        private readonly WavGenerator wavGenerator;

        public void Play()
        {
            if (!IsAvaliable)
                return;
            if (IsPlaying)
                return;
            player.Start();
            IsPlaying = true;
            logger.LogInformationEx($"called.");
        }

        public void Pause()
        {
            if (!IsAvaliable)
                return;
            if (!IsPlaying)
                return;
            player.Pause();
            IsPlaying = false;
            logger.LogInformationEx($"called.");
        }

        public void Stop()
        {
            if (!IsAvaliable)
                return;
            player.Stop();
            player.Prepare(); // 重新准备可再次播放
            IsPlaying = false;
            logger.LogInformationEx($"called.");
        }

        public void Seek(TimeSpan position, bool pause)
        {
            if (!IsAvaliable)
                return;
            logger.LogInformationEx($"position:{position}, pause:{pause}");

            player.SeekTo((int)position.TotalMilliseconds);
            if (pause)
                Pause();
        }

        public void Dispose()
        {
            if (!IsAvaliable)
                return;
            IsAvaliable = false;

            logger.LogInformationEx($"before disposing audioplayer");
            player.Stop();
            player.Release();
            player.Dispose();
            logger.LogInformationEx($"done.");
        }

        public void OnPrepared(MediaPlayer mp)
        {
            if (!prepareTokenSource.Task.IsCompleted)
            {
                IsAvaliable = true;
                Duration = TimeSpan.FromMilliseconds(player.Duration);
                Volume = 1;
                logger.LogInformationEx($"audioPlayer is ready.");
                prepareTokenSource.SetResult();
            }
        }

        public void OnCompletion(MediaPlayer mp)
        {
            Stop();
            logger.LogInformationEx($"audioPlayer playing is done.");
        }
    }
}

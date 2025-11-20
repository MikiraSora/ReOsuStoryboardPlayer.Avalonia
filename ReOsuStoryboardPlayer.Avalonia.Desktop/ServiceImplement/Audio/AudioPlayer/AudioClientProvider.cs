using CommunityToolkit.Mvvm.ComponentModel;
using DirectN;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.Utils;
using ReOsuStoryboardPlayer.Avalonia.Desktop.Utils;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.AudioPlayer
{
    internal partial class AudioClientProvider : ObservableObject, IAudioPlayer
    {
        ILogger<AudioClientProvider> logger;
        public Action removeAction;
        private byte[] pcmFloat;
        private int totalFrames;
        private int cursorFrames;
        private int channels;
        private int sampleRate;
        [ObservableProperty]
        private float volume = 1.0f;
        public int Channels => channels;
        public int SampleRate => sampleRate;
        [ObservableProperty]
        public bool isPlaying;
        [ObservableProperty]
        public bool isAvaliable;
        [ObservableProperty]
        private TimeSpan leadIn;
        private bool disposedValue;
        private TimeSpan stopwatchOffset;
        private long stopwatchTimestamp;
        private Lock playLock;
        private bool mediaend;

        public TimeSpan Duration => TimeSpan.FromSeconds(totalFrames / (double)sampleRate);

        public TimeSpan CurrentTime
        {
            get
            {
                lock (playLock)
                {
                    if (IsPlaying)
                    {
                        return Stopwatch.GetElapsedTime(stopwatchTimestamp) + stopwatchOffset;
                    }
                    else
                    {
                        return TimeSpan.FromSeconds(cursorFrames / (double)sampleRate);
                    }
                }
            }
        }

        public AudioClientProvider(ILogger<AudioClientProvider> logger)
        {
            this.logger = logger;
            playLock = new();
        }

        public void Play()
        {
            lock (playLock)
            {
                if (mediaend)
                {
                    cursorFrames = 0;
                }
                IsPlaying = true;
                stopwatchOffset = TimeSpan.FromSeconds(cursorFrames / (double)sampleRate);
                stopwatchTimestamp = Stopwatch.GetTimestamp();
            }
        }
        public void Pause()
        {
            lock (playLock)
            {
                IsPlaying = false;
            }
        }
        public void Stop()
        {
            lock (playLock)
            {
                IsPlaying = false;
                cursorFrames = 0;
            }
        }

        public void Seek(TimeSpan TimeSpan, bool pause)
        {
            lock (playLock)
            {
                cursorFrames = (int)(TimeSpan.TotalSeconds * sampleRate);
                stopwatchOffset = TimeSpan.FromSeconds(cursorFrames / (double)sampleRate);
                stopwatchTimestamp = Stopwatch.GetTimestamp();
            }
        }

        public async Task<bool> Load(Stream stream, WAVEFORMATEX deviceMix)
        {
            await Task.Run(() =>
            {
                channels = deviceMix.nChannels;
                sampleRate = (int)deviceMix.nSamplesPerSec;
                var reader = MF.MFCreateSourceReaderFromByteStream(stream);
                reader.SetStreamSelection(unchecked((uint)DirectN.MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_ALL_STREAMS), false);
                reader.SetStreamSelection(unchecked((uint)DirectN.MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_FIRST_AUDIO_STREAM), true);
                var outType = MF.MFCreateMediaType();
                outType.SetGUID(MF.MFGuids.MF_MT_MAJOR_TYPE, MF.MFGuids.MFMediaType_Audio);
                outType.SetGUID(MF.MFGuids.MF_MT_SUBTYPE, MF.MFGuids.MFAudioFormat_Float);
                outType.SetUINT32(MF.MFGuids.MF_MT_AUDIO_NUM_CHANNELS, (uint)channels);
                outType.SetUINT32(MF.MFGuids.MF_MT_AUDIO_SAMPLES_PER_SECOND, (uint)sampleRate);
                outType.SetUINT32(MF.MFGuids.MF_MT_AUDIO_BITS_PER_SAMPLE, 32);
                reader.SetCurrentMediaType(unchecked((uint)DirectN.MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_FIRST_AUDIO_STREAM), IntPtr.Zero, outType);
                using var ms = new MemoryStream(capacity: 1 << 20);
                DecodeFloatPcm(reader, ms);
                pcmFloat = ms.ToArray();
                totalFrames = pcmFloat.Length / (channels * sizeof(float));
                cursorFrames = 0;
            });
            OnPropertyChanged(nameof(Duration));
            IsPlaying = false;
            IsAvaliable = true;
            return true;
        }

        private unsafe void DecodeFloatPcm(IMFSourceReader reader, Stream destination)
        {
            int dwFlags = 0;
            nint pSample = 0;
            while (true)
            {
                var hr = reader.ReadSample(
                    unchecked((uint)DirectN.MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_FIRST_AUDIO_STREAM),
                    0, 0, (nint)(&dwFlags), 0, (nint)(&pSample));
                Marshal.ThrowExceptionForHR(hr);
                if ((dwFlags & (int)DirectN.MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED) != 0)
                    throw new NotSupportedException("Type change not supported.");
                if ((dwFlags & (int)DirectN.MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_ENDOFSTREAM) != 0)
                    break;
                if (pSample == 0)
                    continue;
                var sample = COMUtils.GetManaged<IMFSample>(pSample);
                hr = sample.ConvertToContiguousBuffer(out var buffer);
                Marshal.ThrowExceptionForHR(hr);
                int curLen;
                buffer.Lock(out var p, 0, (nint)(&curLen));
                destination.Write(new ReadOnlySpan<byte>((void*)p, curLen));
                buffer.Unlock();
            }
        }

        public int MixIntoFloat(Span<float> mixBuffer, int frames)
        {
            lock (playLock)
            {
                if (!IsPlaying) return 0;
                int remainFrames = totalFrames - cursorFrames;
                if (remainFrames <= 0) { IsPlaying = false; return 0; }
                int framesToMix = Math.Min(frames, remainFrames);
                int srcFloatOffset = cursorFrames * channels;
                int srcFloatCount = framesToMix * channels;
                var src = MemoryMarshal.Cast<byte, float>(pcmFloat.AsSpan()).Slice(srcFloatOffset, srcFloatCount);
                var dst = mixBuffer[..srcFloatCount];
                TensorPrimitives.Multiply(src, Volume, dst);
                cursorFrames += framesToMix;
                if (cursorFrames >= totalFrames) { IsPlaying = false; mediaend = true; }
                return framesToMix;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                removeAction();
                disposedValue = true;
            }
        }
        ~AudioClientProvider()
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

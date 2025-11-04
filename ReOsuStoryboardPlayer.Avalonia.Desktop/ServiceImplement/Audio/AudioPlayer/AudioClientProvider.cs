using CommunityToolkit.Mvvm.ComponentModel;
using DirectN;
using ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.Utils;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.AudioPlayer
{
    internal partial class AudioClientProvider : ObservableObject, IAudioPlayer
    {
        MF MFUtils;
        ComWrappers comWrappers;
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

        public TimeSpan Duration => TimeSpan.FromSeconds(totalFrames / (double)sampleRate);

        public TimeSpan CurrentTime => TimeSpan.FromSeconds(cursorFrames / (double)sampleRate);

        public AudioClientProvider(MF MFUtils, ComWrappers comWrappers)
        {
            this.MFUtils = MFUtils;
            this.comWrappers = comWrappers;
        }

        public void Play() => IsPlaying = true;
        public void Pause() => IsPlaying = false;
        public void Stop() { IsPlaying = false; cursorFrames = 0; }

        public void Seek(TimeSpan TimeSpan, bool pause) { cursorFrames = (int)(TimeSpan.TotalSeconds * sampleRate); IsPlaying = !pause; }

        public async Task<bool> Load(Stream stream, WAVEFORMATEX deviceMix)
        {
            channels = deviceMix.nChannels;
            sampleRate = (int)deviceMix.nSamplesPerSec;
            var reader = MFUtils.MFCreateSourceReaderFromByteStream(stream);
            reader.SetStreamSelection(unchecked((uint)DirectN.MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_ALL_STREAMS), false);
            reader.SetStreamSelection(unchecked((uint)DirectN.MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_FIRST_AUDIO_STREAM), true);
            var outType = MFUtils.MFCreateMediaType();
            outType.SetGUID(MF.MFGuids.MF_MT_MAJOR_TYPE, MF.MFGuids.MFMediaType_Audio);
            outType.SetGUID(MF.MFGuids.MF_MT_SUBTYPE, MF.MFGuids.MFAudioFormat_Float);
            outType.SetUINT32(MF.MFGuids.MF_MT_AUDIO_NUM_CHANNELS, (uint)channels);
            outType.SetUINT32(MF.MFGuids.MF_MT_AUDIO_SAMPLES_PER_BLOCK, (uint)channels);
            outType.SetUINT32(MF.MFGuids.MF_MT_AUDIO_BITS_PER_SAMPLE, (uint)sampleRate);
            reader.SetCurrentMediaType(unchecked((uint)DirectN.MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_FIRST_AUDIO_STREAM), IntPtr.Zero, outType);
            using var ms = new MemoryStream(capacity: 1 << 20);
            DecodeFloatPcm(reader, ms);
            pcmFloat = ms.ToArray();
            totalFrames = pcmFloat.Length / (channels * sizeof(float));
            cursorFrames = 0;
            IsPlaying = false;
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
                var sample = (IMFSample)comWrappers.GetOrCreateObjectForComInstance(pSample, CreateObjectFlags.None);
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
            if (!IsPlaying) return 0;
            var leadInFrames = LeadIn.Seconds * SampleRate;
            if (leadInFrames > 0)
            {
                int consume = Math.Min(frames, leadInFrames);
                leadInFrames -= consume;
                return consume;
            }

            int remainFrames = totalFrames - cursorFrames;
            if (remainFrames <= 0) { IsPlaying = false; return 0; }

            int framesToMix = Math.Min(frames, remainFrames);
            int srcFloatOffset = cursorFrames * channels;
            int srcFloatCount = framesToMix * channels;

            var src = MemoryMarshal.Cast<byte, float>(pcmFloat.AsSpan()).Slice(srcFloatOffset, srcFloatCount);
            var dst = mixBuffer[..srcFloatCount];
            for (int i = 0; i < srcFloatCount; i++)
                dst[i] += src[i] * Volume;

            cursorFrames += framesToMix;
            if (cursorFrames >= totalFrames) IsPlaying = false;
            return framesToMix;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~AudioClientProvider()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

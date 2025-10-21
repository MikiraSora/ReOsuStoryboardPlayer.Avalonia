using Microsoft.Extensions.Logging;
using MP3Sharp;
using NVorbis;
using NWaves.Audio;
using NWaves.Operations;
using NWaves.Signals;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
/*
namespace AcbGeneratorFuck
{
    public static class Generator
    {
        public static Stream ProcessWavFile(WaveFile waveFile, int targetSampleRate, int thread)
        {
            var ms = new MemoryStream();
            if (waveFile.WaveFmt.SamplingRate != targetSampleRate)
            {
                System.Console.WriteLine($"Generate() resampling from {waveFile.WaveFmt.SamplingRate} to 48000.....");
                var resampler = new Resampler();
                var resampledChannels = Enumerable
                    .Range(0, waveFile.WaveFmt.ChannelCount)
                    .Select(channelIdx => resampler.Resample(waveFile[(Channels)channelIdx], targetSampleRate, parallelThread: (uint)thread))
                    .ToArray();

                var resampledWavFile = new WaveFile(resampledChannels);

                resampledWavFile.SaveTo(ms, true);
            }
            else
                waveFile.SaveTo(ms);
            return ms;
        }

        public static WaveFile LoadAsWavFile(string filePath)
        {
            using var fs = File.OpenRead(filePath);

            IEnumerable<T> Skip<T>(T[] arr, int offset, int step)
            {
                for (int i = offset; i < arr.Length; i += step)
                    yield return arr[i];
            }

            if (filePath.EndsWith(".mp3"))
            {
                System.Console.WriteLine($"Generate() converting from mp3 to wav(pcm)..");
                using var mp3Stream = new MP3Stream(fs);

                var sampleRate = mp3Stream.Frequency;
                var channel = mp3Stream.ChannelCount;

                IEnumerable<byte> DumpSamples(MP3Stream file)
                {
                    var buffer = new byte[4096];
                    while (true)
                    {
                        var read = file.Read(buffer, 0, buffer.Length);
                        if (read == 0)
                            break;

                        for (int i = 0; i < read; i++)
                            yield return buffer[i];
                    }
                }

                IEnumerable<float> ConvertBytesToFloat(IEnumerable<byte> byteStream)
                {
                    var itor = byteStream.GetEnumerator();
                    var buf = new byte[2];
                    var i = 0;
                    while (itor.MoveNext())
                    {
                        buf[i++] = itor.Current;
                        if (i == 2)
                        {
                            yield return BitConverter.ToInt16(buf, 0) / 32768f;
                            i = 0;
                        }
                    }
                }

                var sampleData = ConvertBytesToFloat(DumpSamples(mp3Stream)).ToArray();
                var channelSignals = Enumerable
                    .Range(0, channel)
                    .Select(offset => new DiscreteSignal(sampleRate, Skip(sampleData, offset, channel)))
                    .ToArray();
                var waveFile2 = new WaveFile(channelSignals);
                return waveFile2;
            }
            else if (filePath.EndsWith(".ogg"))
            {
                System.Console.WriteLine($"Generate() converting from ogg to wav(pcm)..");
                using var vorbRead = new VorbisReader(fs, false);
                var sampleRate = vorbRead.SampleRate;
                var channels = vorbRead.Channels;

                IEnumerable<float> DumpSamples(VorbisReader file)
                {
                    var buffer = new float[vorbRead.SampleRate * vorbRead.Channels * sizeof(float)];
                    while (true)
                    {
                        var read = vorbRead.ReadSamples(buffer, 0, buffer.Length);
                        if (read == 0)
                            break;

                        for (int i = 0; i < read; i++)
                            yield return buffer[i];
                    }
                }

                var sampleData = DumpSamples(vorbRead).ToArray();
                var channelSignals = Enumerable
                .Range(0, channels)
                    .Select(offset => new DiscreteSignal(sampleRate, Skip(sampleData, offset, channels)))
                    .ToArray();
                var waveFile2 = new WaveFile(channelSignals);
                return waveFile2;
            }
            else
            {
                return new WaveFile(fs);
            }
        }

        public static WaveFile AdjustDuration(WaveFile wavFile, double appendOffset)
        {
            var delaySamples = (int)(wavFile.WaveFmt.SamplingRate * appendOffset);

            var channelSignals = Enumerable
                    .Range(0, wavFile.WaveFmt.ChannelCount)
                    .Select(i => wavFile[(Channels)i])
                    .Select(singal => singal.Delay(delaySamples))
                    .ToArray();

            System.Console.WriteLine($"AdjustDuration() append audio delay {appendOffset}s ({delaySamples} samples)");
            return new WaveFile(channelSignals);
        }

        public static Stream GenerateWavFileStream(string audioFilePath, double appendOffset, int targetSampleRate, int thread)
        {
            var wavFile = LoadAsWavFile(audioFilePath);
            if (appendOffset != 0)
                wavFile = AdjustDuration(wavFile, appendOffset);
            var duration = TimeSpan.FromSeconds(wavFile[Channels.Sum].Duration);
            var stream = ProcessWavFile(wavFile, thread, targetSampleRate);
            stream.Seek(0, SeekOrigin.Begin);
            System.Console.WriteLine($"Generate() converting to hca...");

            return stream;
        }
    }
}
*/

namespace AcbGenerator
{
    [Injectio.Attributes.RegisterSingleton]
    public class WavGenerator
    {
        private readonly ILogger<WavGenerator> logger;

        public WavGenerator(ILogger<WavGenerator> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 将音频流转换为指定采样率的 WAV PCM 流
        /// </summary>
        public Stream ProcessWavFile(WaveFile waveFile, int targetSampleRate, int threads)
        {
            var ms = new MemoryStream();

            if (waveFile.WaveFmt.SamplingRate != targetSampleRate)
            {
                logger.LogInformationEx($"Resampling from {waveFile.WaveFmt.SamplingRate} to {targetSampleRate}...");
                var resampler = new Resampler();

                var resampledChannels = Enumerable.Range(0, waveFile.WaveFmt.ChannelCount)
                    .Select(ch => resampler.Resample(
                        waveFile[(Channels)ch],
                        targetSampleRate,
                        parallelThread: (uint)Math.Max(1, threads)))
                    .ToArray();

                new WaveFile(resampledChannels).SaveTo(ms, true);
            }
            else
            {
                waveFile.SaveTo(ms);
            }

            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        /// <summary>
        /// 从文件加载任意音频为 WaveFile
        /// 支持: wav, mp3, ogg
        /// </summary>
        public WaveFile LoadAsWavFile(Stream fs)
        {
            try
            {
                return LoadMp3(fs);
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e, "Failed to load as MP3, trying OGG...");
            }

            try
            {
                fs.Seek(0, SeekOrigin.Begin);
                return LoadOgg(fs);
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e, "Failed to load as OGG, trying WAV...");
            }

            try
            {
                fs.Seek(0, SeekOrigin.Begin);
                return new WaveFile(fs);
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e, "Failed to load as WAV");
            }

            return null;
        }

        private WaveFile LoadMp3(Stream fs)
        {
            logger.LogInformationEx("Decoding MP3 → PCM...");

            using var mp3 = new MP3Stream(fs);
            int sampleRate = mp3.Frequency;
            int channels = mp3.ChannelCount;

            var samples = new List<float>(sampleRate * channels * 10); // 预分配一点空间
            var buffer = new byte[4096];

            int read;
            while ((read = mp3.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i += 2)
                    samples.Add(BitConverter.ToInt16(buffer, i) / 32768f);
            }

            return MakeWaveFile(samples, sampleRate, channels);
        }

        private WaveFile LoadOgg(Stream fs)
        {
            logger.LogInformationEx("Decoding OGG → PCM...");

            using var vorbis = new VorbisReader(fs, false);
            int sampleRate = vorbis.SampleRate;
            int channels = vorbis.Channels;

            var samples = new List<float>(sampleRate * channels * 10);
            var buffer = new float[4096 * channels];

            int read;
            while ((read = vorbis.ReadSamples(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                    samples.Add(buffer[i]);
            }

            return MakeWaveFile(samples, sampleRate, channels);
        }

        private WaveFile MakeWaveFile(List<float> samples, int sampleRate, int channels)
        {
            IEnumerable<float> ChannelIterator(int offset)
            {
                for (int i = offset; i < samples.Count; i += channels)
                    yield return samples[i];
            }

            var signals = Enumerable.Range(0, channels)
                .Select(ch => new DiscreteSignal(sampleRate, ChannelIterator(ch)))
                .ToArray();

            return new WaveFile(signals);
        }

        /// <summary>
        /// 在音频前追加静音以调整时长
        /// </summary>
        public WaveFile AdjustDuration(WaveFile wavFile, double appendOffset)
        {
            if (appendOffset <= 0)
                return wavFile;

            int delaySamples = (int)(wavFile.WaveFmt.SamplingRate * appendOffset);

            logger.LogInformationEx($"Appending {appendOffset:0.###}s delay ({delaySamples} samples).");

            var delayed = Enumerable.Range(0, wavFile.WaveFmt.ChannelCount)
                .Select(i => wavFile[(Channels)i].Delay(delaySamples))
                .ToArray();

            return new WaveFile(delayed);
        }

        /// <summary>
        /// 从音频文件生成 WAV 流
        /// </summary>
        public Stream GenerateWavFileStream(Stream audioFileStream, double appendOffset, int targetSampleRate, int threads)
        {
            var wav = LoadAsWavFile(audioFileStream);
            if (appendOffset != 0)
                wav = AdjustDuration(wav, appendOffset);

            var duration = TimeSpan.FromSeconds(wav[Channels.Sum].Duration);
            logger.LogInformationEx($"Duration: {duration.TotalSeconds:0.###}s");

            var stream = ProcessWavFile(wav, targetSampleRate, threads);

            logger.LogInformationEx("WAV generation complete.");
            return stream;
        }
    }
}
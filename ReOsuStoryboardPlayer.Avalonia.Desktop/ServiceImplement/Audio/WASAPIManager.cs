using CommunityToolkit.HighPerformance;
using DirectN;
using Injectio.Attributes;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.AudioPlayer;
using ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.Utils;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio
{
    [RegisterSingleton<IAudioManager>]
    internal partial class WASAPIManager : IAudioManager
    {
        public WASAPIManager(ILogger<WASAPIManager> logger)
        {
            this.logger = logger;
            comWrappers = new StrategyBasedComWrappers();
            isRunning = true;
            eventWaitHandle = new EventWaitHandle(true, EventResetMode.AutoReset);
            MFUtils = new MF(comWrappers);
            players = new();
            playerLock = new Lock();
            Init();
        }
        private MF MFUtils;
        private ILogger<WASAPIManager> logger;
        private ComWrappers comWrappers;
        private IMMDevice mmDevice;
        private IAudioClient3 audioClient;
        private IAudioRenderClient audioRenderClient;
        private WAVEFORMATEX mixWaveFormatEx;
        private EventWaitHandle eventWaitHandle;
        private Thread audioThread;
        private volatile bool isRunning;
        private byte[] audioBuffer;
        private uint frameSize;
        private uint bufferFrameCount;
        private int outChannels;
        private bool outIsFloat;
        private float[] mixScratchFloat;
        private Lock playerLock;

        private List<AudioClientProvider> players;
        public async Task<IAudioPlayer> LoadAudio(Stream stream, double prependLeadInSeconds)
        {
            AudioClientProvider client = new(MFUtils,comWrappers);
            await client.Load(stream, mixWaveFormatEx);
            lock (playerLock)
            {
                players.Add(client);
            }
            return client;
        }

        private void Init()
        {
            mmDevice = GetDefaultAudioEndpoint();
            var hr = mmDevice.Activate(typeof(IAudioClient3).GUID, DirectN.CLSCTX.CLSCTX_ALL, 0, out var audioClientPtr);
            Marshal.ThrowExceptionForHR(hr);
            audioClient = comWrappers.GetOrCreateObjectForComInstance(audioClientPtr, CreateObjectFlags.None) as IAudioClient3;
            hr = audioClient.GetMixFormat(out var mixFormatPtr);
            Marshal.ThrowExceptionForHR(hr);
            mixWaveFormatEx = Marshal.PtrToStructure<WAVEFORMATEX>(mixFormatPtr);
            Marshal.FreeCoTaskMem(mixFormatPtr);
            mixWaveFormatEx = CreateFormat(mixWaveFormatEx.nSamplesPerSec, mixWaveFormatEx.wBitsPerSample, mixWaveFormatEx.nChannels);
            outChannels = mixWaveFormatEx.nChannels;
            outIsFloat = (mixWaveFormatEx.wFormatTag == 3);
            frameSize = (uint)mixWaveFormatEx.nBlockAlign;
            hr = audioClient.GetSharedModeEnginePeriod(mixWaveFormatEx, out var defaultPeriod, out var fundamentalPeriod, out var minPeriod, out var maxPeriod);
            Marshal.ThrowExceptionForHR(hr);
            hr = audioClient.InitializeSharedAudioStream(0x00040000, minPeriod, mixWaveFormatEx, 0);
            Marshal.ThrowExceptionForHR(hr);
            hr = audioClient.SetEventHandle(eventWaitHandle.Handle);
            Marshal.ThrowExceptionForHR(hr);
            hr = audioClient.GetService(typeof(IAudioRenderClient).GUID, out var audioRenderClientPtr);
            Marshal.ThrowExceptionForHR(hr);
            audioRenderClient = comWrappers.GetOrCreateObjectForComInstance(audioRenderClientPtr, CreateObjectFlags.None) as IAudioRenderClient;
            hr = audioClient.Start();
            Marshal.ThrowExceptionForHR(hr);
            hr = audioClient.GetBufferSize(out bufferFrameCount);
            Marshal.ThrowExceptionForHR(hr);
            audioBuffer = new byte[bufferFrameCount * mixWaveFormatEx.nBlockAlign];
            mixScratchFloat = new float[bufferFrameCount * outChannels];
            audioThread = new Thread(AudioThreadEntryPoint)
            {
                IsBackground = true,
                Name = "WASAPI Audio Thread"
            };
            audioThread.Start();
        }

        private unsafe void AudioThreadEntryPoint()
        {
            uint taskIndex = 0;
            IntPtr mmcssHandle = AvSetMmThreadCharacteristicsW("Pro Audio", out taskIndex);
            while (isRunning)
            {
                eventWaitHandle.WaitOne();
                uint currentPadding = 0;
                var hr = audioClient.GetCurrentPadding(out currentPadding);
                uint framesToWrite = bufferFrameCount - currentPadding;
                if (framesToWrite == 0)
                    continue;
                int samples = (int)(framesToWrite * (uint)outChannels);
                var mixSpan = mixScratchFloat.AsSpan(0, samples);
                mixSpan.Clear();
                lock (playerLock)
                {
                    var playerSpan = CollectionsMarshal.AsSpan(players);
                    for (var i = 0; i < playerSpan.Length; i++)
                    {
                        var player = playerSpan[i];
                        player.MixIntoFloat(mixSpan, (int)framesToWrite);
                    }
                }
                for (int i = 0; i < mixSpan.Length; i++)
                {
                    mixSpan[i] = float.Clamp(mixSpan[i], -1f, 1f);
                }
                hr = audioRenderClient.GetBuffer(framesToWrite, out var bufferPtr);
                Span<byte> buffer = new((void*)bufferPtr, (int)(framesToWrite*frameSize));
                if (outIsFloat)
                {
                    MemoryMarshal.Cast<float,byte>(mixSpan).CopyTo(buffer);
                }
                else
                {
                    if (mixWaveFormatEx.wBitsPerSample == 16)
                    {
                        var frameBuffer = MemoryMarshal.Cast<byte, ushort>(buffer);
                        for (int i = 0; i < mixSpan.Length; i++)
                        {
                            frameBuffer[i] = (ushort)(mixSpan[i]*ushort.MaxValue);
                        }
                    }
                    else
                    {
                        
                    }
                }
                hr = audioRenderClient.ReleaseBuffer(framesToWrite, 0);
            }
        }

        private static DirectN.WAVEFORMATEX CreateFormat(uint rate, int bits, int channels)
        {
            ushort formatTag = (ushort)(bits == 32 ? 3 : 1);
            var blockAlign = (ushort)(channels * bits / 8);
            var avgBytesPerSec = rate * blockAlign;
            return new DirectN.WAVEFORMATEX
            {
                wFormatTag = formatTag,
                nChannels = (ushort)channels,
                nSamplesPerSec = rate,
                nAvgBytesPerSec = avgBytesPerSec,
                nBlockAlign = blockAlign,
                wBitsPerSample = (ushort)bits,
                cbSize = 0
            };
        }

        private IMMDevice GetDefaultAudioEndpoint()
        {
            IMMDevice device;
            var hr = ActivateMMDeviceEnumerator().GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out device);
            Marshal.ThrowExceptionForHR(hr);
            return device;
        }

        private IMMDeviceEnumerator ActivateMMDeviceEnumerator()
        {
            return ActivateClass<IMMDeviceEnumerator>(new("BCDE0395-E52F-467C-8E3D-C4579291692E"), new("A95664D2-9614-4F35-A746-DE8DB63617E6"));
        }

        private I ActivateClass<I>(Guid clsid, Guid iid) where I : class
        {
            Debug.Assert(iid == typeof(I).GUID);
            int hr = CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out IntPtr obj);
            Marshal.ThrowExceptionForHR(hr);
            return comWrappers.GetOrCreateObjectForComInstance(obj, CreateObjectFlags.None) as I;
        }

        [LibraryImport("Ole32")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static partial int CoCreateInstance(
            ref Guid rclsid,
            IntPtr pUnkOuter,
            int dwClsContext,
            ref Guid riid,
            out IntPtr ppObj);

        [LibraryImport("Avrt.dll", SetLastError = true)]
        private static partial IntPtr AvSetMmThreadCharacteristicsW([MarshalAs(UnmanagedType.LPWStr)] string taskName, out uint taskIndex);
    }
}

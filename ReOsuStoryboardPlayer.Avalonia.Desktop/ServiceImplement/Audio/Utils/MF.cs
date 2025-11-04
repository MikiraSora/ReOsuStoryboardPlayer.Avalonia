using CommunityToolkit.HighPerformance;
using DirectN;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.Utils
{
    internal partial class MF
    {
        private ComWrappers comWrappers;
        public MF(ComWrappers comWrappers)
        {
            this.comWrappers = comWrappers;
            var hr = MFStartup(0x20070, 0);
        }

        public static class MFGuids
        {
            // attribute keys
            public static readonly Guid MF_MT_MAJOR_TYPE = new("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
            public static readonly Guid MF_MT_SUBTYPE = new("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");

            // major/subtypes
            public static readonly Guid MFMediaType_Audio = new("73647561-0000-0010-8000-00aa00389b71");   // 'audio'
            public static readonly Guid MFAudioFormat_PCM = new("00000001-0000-0010-8000-00aa00389b71");   // PCM 16-bit
            public static readonly Guid MFAudioFormat_Float = new("00000003-0000-0010-8000-00aa00389b71"); // PCM float32


            public static readonly Guid MF_MT_AUDIO_NUM_CHANNELS = new("37e48bf5-645e-4c5b-89de-ada9e29b696a");
            public static readonly Guid MF_MT_AUDIO_SAMPLES_PER_SECOND = new("5faeeae7-0290-4c31-9e8a-c534f68d9dba");
            public static readonly Guid MF_MT_AUDIO_FLOAT_SAMPLES_PER_SECOND = new("fb3b724a-cfb5-4319-aefe-6e42b2406132");
            public static readonly Guid MF_MT_AUDIO_AVG_BYTES_PER_SECOND = new("1aab75c8-cfef-451c-ab95-ac034b8e1731");
            public static readonly Guid MF_MT_AUDIO_BLOCK_ALIGNMENT = new("322de230-9eeb-43bd-ab7a-ff412251541d");
            public static readonly Guid MF_MT_AUDIO_BITS_PER_SAMPLE = new("f2deb57f-40fa-4764-aa33-ed4f2d1ff669");
            public static readonly Guid MF_MT_AUDIO_VALID_BITS_PER_SAMPLE = new("d9bf8d6a-9530-4b7c-9ddf-ff6fd58bbd06");
            public static readonly Guid MF_MT_AUDIO_SAMPLES_PER_BLOCK = new("aab15aac-e13a-4995-9222-501ea15c6877");
            public static readonly Guid MF_MT_AUDIO_CHANNEL_MASK = new("55fb5765-644a-4caf-8479-938983bb1588");
        }

        public IMFSourceReader MFCreateSourceReaderFromByteStream(Stream stream)
        {
            ManagedMFByteStream managedMFByteStream = new ManagedMFByteStream(stream, comWrappers);
            IntPtr pByteStream = comWrappers.GetOrCreateComInterfaceForObject(managedMFByteStream, CreateComInterfaceFlags.None);
            var hr = MFCreateSourceReaderFromByteStream(pByteStream, IntPtr.Zero, out IntPtr ppSourceReader);
            Marshal.ThrowExceptionForHR(hr);
            return comWrappers.GetOrCreateObjectForComInstance(ppSourceReader, CreateObjectFlags.None) as IMFSourceReader;
        }

        public IMFMediaType MFCreateMediaType()
        {
            var hr = MFCreateMediaType(out var ppMFType);
            Marshal.ThrowExceptionForHR(hr);
            return comWrappers.GetOrCreateObjectForComInstance(ppMFType, CreateObjectFlags.None) as IMFMediaType;
        }

        [LibraryImport("Mfplat.dll")]
        public static partial int MFStartup(int version, int dwFlags);

        [LibraryImport("Mfplat.dll")]
        public static partial int MFShutdown();

        [LibraryImport("Mfplat.dll")]
        public static partial int MFCreateMediaType(out IntPtr ppMFType);

        [LibraryImport("Mfreadwrite.dll")]
        public static partial int MFCreateSourceReaderFromByteStream(IntPtr pByteStream, IntPtr pAttributes, out IntPtr ppSourceReader);
        
        [Flags]
        internal enum Capabilities
        {
            Readable = 0x1,
            Writable = 0x2,
            Seekable = 0x4,
            Remote = 0x8,
            Directory = 0x80,
            SlowSeek = 0x100,
            PartiallyDownloaded = 0x200,
            ShareWrite = 0x400,
            DoseNotUseNetwork = 0x800,
        }

        internal enum MFByteStreamSeekOrigin
        {
            Begin = 0,
            Current
        }

        [GeneratedComInterface]
        [Guid("a27003cf-2354-4f2a-8d6a-ab7cff15437e")]
        internal partial interface IMFAsyncCallback
        {
            [PreserveSig]
            int GetParameters(out int pdwFlags, out int pdwQueue);
            void Invoke(IntPtr pAsyncResult);
        }

        [GeneratedComInterface]
        [Guid("ac6b7889-0740-4d51-8619-905994a55cc6")]
        internal partial interface IMFAsyncResult
        {
            IntPtr GetState();
            [PreserveSig]
            int GetStatus();
            void SetStates(int hrStatus);
            IntPtr GetObject();
            [PreserveSig]
            IntPtr GetStateNoAddRef();
            int _Result(); //在最后偷偷藏一个自己用的方法

        }

        [GeneratedComClass]
        internal partial class ManagedMFAsyncResult : IMFAsyncResult
        {
            private readonly IntPtr _state;
            private int hr;
            private int result;

            public ManagedMFAsyncResult(IntPtr state, int result)
            {
                _state = state;
                this.result = result;
            }

            public nint GetObject()
            {
                return 0;
            }

            public nint GetState()
            {
                if (_state != IntPtr.Zero) Marshal.AddRef(_state);
                return _state;
            }

            public nint GetStateNoAddRef()
            {
                return _state;
            }

            public int GetStatus()
            {
                return hr;
            }

            public void SetStates(int hrStatus)
            {
                hr = hrStatus;
            }

            public int _Result()
            {
                return result;
            }
        }

        [GeneratedComInterface]
        [Guid("ad4c1b00-4bf7-422f-9175-756693d9130d")]
        internal partial interface IMFByteStream
        {
            Capabilities GetCapabilities();
            long GetLength();
            void SetLength(long length);
            long GetCurrentPosition();
            void SetCurrentPosition(long position);
            [return:MarshalAs(UnmanagedType.Bool)]
            bool IsEndOfStream();
            int Read(IntPtr pb, int cb);
            void BeginRead(IntPtr pb, int cb, IMFAsyncCallback pCallback, IntPtr punkState);
            int EndRead(IMFAsyncResult pResult);
            int Write(IntPtr pb, int cb);
            void BeginWrite(IntPtr pb, int cb, IMFAsyncCallback pCallback, IntPtr punkState);
            int EndWrite(IMFAsyncResult pResult);
            long Seek(MFByteStreamSeekOrigin seekOrigin, long seekOffset,int seekFlags);
            void Flush();
            void Close();
        }

        [GeneratedComClass]
        internal partial class ManagedMFByteStream : IMFByteStream
        {
            private Stream stream;
            private ComWrappers comWrappers;
            public ManagedMFByteStream(Stream stream,ComWrappers comWrappers)
            {
                this.stream = stream;
                this.comWrappers = comWrappers;
            }
            public unsafe void BeginRead(nint pb, int cb, IMFAsyncCallback pCallback, nint punkState)
            {
                __MemoryManager __memoryManager = new __MemoryManager(pb, cb);
                
                stream.ReadAsync(__memoryManager.Memory).AsTask().ContinueWith(t =>
                {
                    int readBytes = t.Result;
                    ManagedMFAsyncResult asyncResult = new ManagedMFAsyncResult(punkState, readBytes);
                    pCallback.Invoke(comWrappers.GetOrCreateComInterfaceForObject(asyncResult, CreateComInterfaceFlags.None));
                });
            }

            public void BeginWrite(nint pb, int cb, IMFAsyncCallback pCallback, nint punkState)
            {
                __MemoryManager __memoryManager = new __MemoryManager(pb, cb);
                stream.WriteAsync(__memoryManager.Memory).AsTask().ContinueWith(t =>
                {
                    ManagedMFAsyncResult asyncResult = new ManagedMFAsyncResult(punkState, cb);
                    pCallback.Invoke(comWrappers.GetOrCreateComInterfaceForObject(asyncResult, CreateComInterfaceFlags.None));
                });
            }

            public void Close()
            {
                stream.Close();
            }

            public int EndRead(IMFAsyncResult pResult)
            {
                return pResult._Result();
            }

            public int EndWrite(IMFAsyncResult pResult)
            {
                return pResult._Result();
            }

            public void Flush()
            {
                stream.Flush();
            }

            public Capabilities GetCapabilities()
            {
                Capabilities flags = (stream.CanRead ? Capabilities.Readable : 0)| (stream.CanWrite ? Capabilities.Writable : 0) | (stream.CanSeek ? Capabilities.Seekable : 0);
                flags |= Capabilities.DoseNotUseNetwork;
                return flags;
            }

            public long GetCurrentPosition()
            {
                return stream.Position;
            }

            public long GetLength()
            {
                return stream.Length;
            }

            [return: MarshalAs(UnmanagedType.Bool)]
            public bool IsEndOfStream()
            {
                return stream.Position >= stream.Length;
            }

            public unsafe int Read(nint pb, int cb)
            {
                return stream.Read(new Span<byte>((void*)pb, cb));
            }

            public long Seek(MFByteStreamSeekOrigin seekOrigin, long seekOffset, int seekFlags)
            {
                return stream.Seek(seekOffset, seekOrigin == MFByteStreamSeekOrigin.Begin ? SeekOrigin.Begin : SeekOrigin.Current);
            }

            public void SetCurrentPosition(long position)
            {
                stream.Position = position;
            }

            public void SetLength(long length)
            {
                stream.SetLength(length);
            }

            public unsafe int Write(nint pb, int cb)
            {
                stream.Write(new ReadOnlySpan<byte>((void*)pb, cb));
                return cb;
            }
        }

        public class __MemoryManager : MemoryManager<byte>
        {
            private IntPtr ptr;
            private int length;
            public __MemoryManager(IntPtr ptr,int length)
            {
                this.ptr = ptr;
                this.length = length;
            }

            public unsafe override Span<byte> GetSpan()
            {
                return new Span<byte>(ptr.ToPointer(), length);
            }

            public unsafe override MemoryHandle Pin(int elementIndex = 0)
            {
                return new MemoryHandle((void*)(ptr + elementIndex));
            }

            public override void Unpin()
            {
            }

            protected override void Dispose(bool disposing)
            {
            }
        }
    }
}

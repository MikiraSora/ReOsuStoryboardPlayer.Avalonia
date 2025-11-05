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
using static ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.Utils.MF;

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
            public static readonly Guid MF_MT_MAJOR_TYPE = new("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
            public static readonly Guid MF_MT_SUBTYPE = new("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");

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
            //ManagedMFByteStream managedMFByteStream = new ManagedMFByteStream(stream);
            ManagedIStream managedIStream = new ManagedIStream(stream);
            MFCreateMFByteStreamOnStream(managedIStream, out var byteStream);
            var hr = MFCreateSourceReaderFromByteStream(byteStream, IntPtr.Zero, out var ppSourceReader);
            Marshal.ThrowExceptionForHR(hr);
            return ppSourceReader;
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

        [LibraryImport("Mfplat.dll")]
        public static partial int MFCreateMFByteStreamOnStream([MarshalUsing(typeof(UniqueComInterfaceMarshaller<IStream>))] IStream stream, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IMFByteStream>))] out IMFByteStream byteStream);

        [LibraryImport("Mfreadwrite.dll")]
        public static partial int MFCreateSourceReaderFromByteStream([MarshalUsing(typeof(UniqueComInterfaceMarshaller<IMFByteStream>))] IMFByteStream pByteStream, IntPtr pAttributes, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IMFSourceReader>))] out IMFSourceReader ppSourceReader);

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

        [GeneratedComClass]
        internal partial class ManagedIStream : IStream
        {
            private Stream _stream;

            public ManagedIStream(Stream stream)
            {
                _stream = stream;
            }

            public HRESULT Clone([MarshalUsing(typeof(UniqueComInterfaceMarshaller<IStream>))] out IStream ppstm)
            {
                ppstm = null;
                return 0x80004001;
            }

            public HRESULT Commit(uint grfCommitFlags)
            {
                _stream.Flush();
                return 0;
            }

            public HRESULT CopyTo([MarshalUsing(typeof(UniqueComInterfaceMarshaller<IStream>))] IStream pstm, ulong cb, nint pcbRead, nint pcbWritten)
            {
                return 0x80004001;
            }

            public HRESULT LockRegion(ulong libOffset, ulong cb, uint dwLockType)
            {
                return 0;
            }

            public unsafe HRESULT Read(nint pv, uint cb, nint pcbRead)
            {
                *(int*)(pcbRead) = _stream.Read(new Span<byte>(pv.ToPointer(), (int)cb));
                return 0;
            }

            public HRESULT Revert()
            {
                return 0;
            }

            public unsafe HRESULT Seek(long dlibMove, STREAM_SEEK dwOrigin, nint plibNewPosition)
            {
                var result = _stream.Seek(dlibMove, (SeekOrigin)dwOrigin);
                if (plibNewPosition != 0)
                {
                    *(long*)(plibNewPosition) = result;
                }
                return 0;
            }

            public HRESULT SetSize(ulong libNewSize)
            {
                _stream.SetLength((long)libNewSize);
                return 0;
            }

            public HRESULT Stat(out STATSTG pstatstg, uint grfStatFlag)
            {
                pstatstg = new()
                {
                    type = 2,
                    cbSize = (ulong)_stream.Length,
                };
                return 0;
            }

            public HRESULT UnlockRegion(ulong libOffset, ulong cb, uint dwLockType)
            {
                return 0;
            }

            public unsafe HRESULT Write(nint pv, uint cb, nint pcbWritten)
            {
                _stream.Write(new Span<byte>(pv.ToPointer(), (int)cb));
                if (pcbWritten != 0)
                {
                    *(uint*)(pcbWritten) = cb;
                }
                return 0;
            }
        }

        [GeneratedComClass]
        internal partial class ManagedMFAsyncResult : IMFAsyncResult
        {
            private readonly IntPtr _state;
            private int hr;
            private uint result;

            public ManagedMFAsyncResult(IntPtr state, uint result)
            {
                _state = state;
                this.result = result;
            }

            public HRESULT GetObject(out nint ppObject)
            {
                ppObject = Marshal.AllocCoTaskMem(4);
                Marshal.WriteInt32(ppObject, (int)result);
                return 0;
            }

            public HRESULT GetState(out nint ppunkState)
            {
                if (_state != IntPtr.Zero) Marshal.AddRef(_state);
                ppunkState = _state;
                return 0;
            }

            public nint GetStateNoAddRef()
            {
                return _state;
            }

            public void SetStates(int hrStatus)
            {
                hr = hrStatus;
            }

            public HRESULT SetStatus(HRESULT hrStatus)
            {
                hr = hrStatus;
                return 0;
            }

            public HRESULT GetStatus()
            {
                return hr;
            }

            public uint GetResult()
            {
                return result;
            }
        }

        [GeneratedComClass]
        internal partial class ManagedMFByteStream : IMFByteStream
        {
            private Stream stream;
            public ManagedMFByteStream(Stream stream)
            {
                this.stream = stream;
            }

            public HRESULT BeginRead(nint pb, uint cb, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IMFAsyncCallback>))] IMFAsyncCallback pCallback, nint punkState)
            {
                __MemoryManager __memoryManager = new __MemoryManager(pb, cb);
                stream.ReadAsync(__memoryManager.Memory).AsTask().ContinueWith(t =>
                {
                    int readBytes = t.Result;
                    ManagedMFAsyncResult asyncResult = new(punkState, (uint)readBytes);
                    pCallback.Invoke(asyncResult);
                });
                return 0;
            }

            public HRESULT BeginWrite(nint pb, uint cb, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IMFAsyncCallback>))] IMFAsyncCallback pCallback, nint punkState)
            {
                __MemoryManager __memoryManager = new __MemoryManager(pb, cb);
                stream.WriteAsync(__memoryManager.Memory).AsTask().ContinueWith(t =>
                {
                    ManagedMFAsyncResult asyncResult = new ManagedMFAsyncResult(punkState, cb);
                    pCallback.Invoke(asyncResult);
                });
                return 0;
            }

            public HRESULT EndRead([MarshalUsing(typeof(UniqueComInterfaceMarshaller<IMFAsyncResult>))] IMFAsyncResult pResult, out uint pcbRead)
            {
                pResult.GetObject(out var ppObject);
                pcbRead = (uint)Marshal.ReadInt32(ppObject);
                Marshal.FreeCoTaskMem(ppObject);
                return 0;
            }

            public HRESULT EndWrite([MarshalUsing(typeof(UniqueComInterfaceMarshaller<IMFAsyncResult>))] IMFAsyncResult pResult, out uint pcbWritten)
            {
                pResult.GetObject(out var ppObject);
                pcbWritten = (uint)Marshal.ReadInt32(ppObject);
                Marshal.FreeCoTaskMem(ppObject);
                return 0;
            }

            public HRESULT GetCapabilities(out uint pdwCapabilities)
            {
                Capabilities flags = (stream.CanRead ? Capabilities.Readable : 0) | (stream.CanWrite ? Capabilities.Writable : 0) | (stream.CanSeek ? Capabilities.Seekable : 0);
                flags |= Capabilities.DoseNotUseNetwork;
                pdwCapabilities = (uint)flags;
                return 0;
            }

            public HRESULT GetCurrentPosition(out ulong pqwPosition)
            {
                pqwPosition = (ulong)stream.Position;
                return 0;
            }


            public HRESULT GetLength(out ulong pqwLength)
            {
                pqwLength = (ulong)stream.Length;
                return 0;
            }

            public HRESULT IsEndOfStream(out BOOL pfEndOfStream)
            {
                pfEndOfStream = stream.Position >= stream.Length;
                return 0;
            }

            public unsafe HRESULT Read(nint pb, uint cb, out uint pcbRead)
            {
                pcbRead = (uint)stream.Read(new Span<byte>((void*)pb, (int)cb));
                return 0;
            }

            public HRESULT Seek(MFBYTESTREAM_SEEK_ORIGIN seekOrigin, long llSeekOffset, uint dwSeekFlags, out ulong pqwCurrentPosition)
            {
                pqwCurrentPosition = (ulong)stream.Seek(llSeekOffset, seekOrigin == MFBYTESTREAM_SEEK_ORIGIN.msoBegin ? SeekOrigin.Begin : SeekOrigin.Current);
                return 0;
            }

            public HRESULT SetCurrentPosition(ulong qwPosition)
            {
                stream.Position = (long)qwPosition;
                return 0;
            }

            public HRESULT SetLength(ulong qwLength)
            {
                stream.SetLength((long)qwLength);
                return 0;
            }

            public unsafe HRESULT Write(nint pb, uint cb, out uint pcbWritten)
            {
                stream.Write(new ReadOnlySpan<byte>((void*)pb, (int)cb));
                pcbWritten = cb;
                return 0;
            }

            public HRESULT Close()
            {
                stream.Close();
                return 0;
            }

            public HRESULT Flush()
            {
                stream.Flush();
                return 0;
            }
        }

        public class __MemoryManager : MemoryManager<byte>
        {
            private IntPtr ptr;
            private uint length;
            public __MemoryManager(IntPtr ptr, uint length)
            {
                this.ptr = ptr;
                this.length = length;
            }

            public unsafe override Span<byte> GetSpan()
            {
                return new Span<byte>(ptr.ToPointer(), (int)length);
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

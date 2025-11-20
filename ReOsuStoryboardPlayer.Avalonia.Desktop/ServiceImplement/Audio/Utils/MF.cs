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
    internal partial class MF : IDisposable
    {
        private bool disposedValue;

        public MF()
        {
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

        public static IMFSourceReader MFCreateSourceReaderFromByteStream(Stream stream)
        {
            //ManagedMFByteStream managedMFByteStream = new ManagedMFByteStream(stream);
            ManagedIStream managedIStream = new(stream);
            MFCreateMFByteStreamOnStream(managedIStream, out var byteStream);
            var hr = MFCreateSourceReaderFromByteStream(byteStream, IntPtr.Zero, out var ppSourceReader);
            Marshal.ThrowExceptionForHR(hr);
            return ppSourceReader;
        }

        public static IMFMediaType MFCreateMediaType()
        {
            var hr = MFCreateMediaType(out var ppMFType);
            Marshal.ThrowExceptionForHR(hr);
            return ppMFType;
        }

        [LibraryImport("Mfplat.dll")]
        public static partial int MFStartup(int version, int dwFlags);

        [LibraryImport("Mfplat.dll")]
        public static partial int MFShutdown();

        [LibraryImport("Mfplat.dll")]
        public static partial int MFCreateMediaType([MarshalUsing(typeof(UniqueComInterfaceMarshaller<IMFMediaType>))] out IMFMediaType ppMFType);

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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                MFShutdown();
                disposedValue = true;
            }
        }

        ~MF()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

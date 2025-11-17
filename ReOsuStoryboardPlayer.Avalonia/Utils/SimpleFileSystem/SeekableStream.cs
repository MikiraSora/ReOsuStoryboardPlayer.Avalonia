using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem
{
    internal class SeekableStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly List<byte> _cache;
        private long _position;
        private long _cachedLength;
        private bool _endOfStream;
        private readonly long _length;

        public SeekableStream(Stream baseStream,long length)
        {
            ArgumentNullException.ThrowIfNull(baseStream);
            if (!baseStream.CanRead)
                throw new ArgumentException("Base stream must be readable", nameof(baseStream));
            _baseStream = baseStream;
            _cache = [];
            _position = 0;
            _cachedLength = 0;
            _endOfStream = false;
            _length = length;
        }

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                return _length;
            }
        }

        public override long Position
        {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException(offset < 0 ? nameof(offset) : nameof(count));
            if (offset + count > buffer.Length)
                throw new ArgumentException("Invalid offset and count");
            if (count == 0)
                return 0;
            long targetPosition = _position + count;
            EnsureCached(targetPosition);
            long available = _cachedLength - _position;
            int bytesToRead = (int)Math.Min(available, count);

            if (bytesToRead > 0)
            {
                var cacheSpan = CollectionsMarshal.AsSpan(_cache);
                var sourceSpan = cacheSpan.Slice((int)_position, bytesToRead);
                var destSpan = new Span<byte>(buffer, offset, bytesToRead);
                sourceSpan.CopyTo(destSpan);
            }

            _position += bytesToRead;
            return bytesToRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = _position + offset;
                    break;
                case SeekOrigin.End:
                    newPosition = Length + offset;
                    break;
                default:
                    throw new ArgumentException("Invalid seek origin", nameof(origin));
            }
            if (newPosition < 0)
                throw new IOException("Cannot seek to negative position");

            _position = newPosition;
            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("SeekableStream does not support SetLength");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("SeekableStream does not support Write");
        }

        private void EnsureCached(long position)
        {
            if (position <= _cachedLength || _endOfStream)
                return;
            long bytesToRead = position - _cachedLength;
            byte[] buffer = ArrayPool<byte>.Shared.Rent((int)Math.Min(bytesToRead, 8192));

            while (_cachedLength < position && !_endOfStream)
            {
                int bytesRead = _baseStream.Read(buffer, 0, (int)Math.Min(buffer.Length, position - _cachedLength));
                
                if (bytesRead > 0)
                {
                    _cache.AddRange(buffer.AsSpan(0, bytesRead));
                    _cachedLength += bytesRead;
                }
                else
                {
                    _endOfStream = true;
                    break;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _baseStream?.Dispose();
                _cache?.Clear();
            }
            base.Dispose(disposing);
        }
    }
}

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic
{
    public class ChunkedMemoryStream : Stream
    {
        private readonly ArrayPool<byte> _arrayPool;
        private readonly List<byte[]> _chunks;
        private long _length;
        private long _position;
        private readonly int _chunkSize;

        public ChunkedMemoryStream(int capacity, int chunkSize = 1024 * 50)
        {
            _arrayPool = ArrayPool<byte>.Shared;
            _chunks = new List<byte[]>(capacity / chunkSize);
            _chunkSize = chunkSize;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        public override void Flush() { } // No-op for in-memory stream

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            while (count > 0 && _position < _length)
            {
                int currentChunkIndex = (int)(_position / _chunkSize);
                int currentChunkOffset = (int)(_position % _chunkSize);
                byte[] currentChunk = _chunks[currentChunkIndex];

                int bytesToReadInChunk = Math.Min(count, _chunkSize - currentChunkOffset);
                bytesToReadInChunk = (int)Math.Min(bytesToReadInChunk, _length - _position);

                Buffer.BlockCopy(currentChunk, currentChunkOffset, buffer, offset, bytesToReadInChunk);

                _position += bytesToReadInChunk;
                offset += bytesToReadInChunk;
                count -= bytesToReadInChunk;
                bytesRead += bytesToReadInChunk;
            }
            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int currentChunkIndex = (int)(_position / _chunkSize);
                int currentChunkOffset = (int)(_position % _chunkSize);

                // Ensure chunk exists and is large enough
                if (currentChunkIndex >= _chunks.Count)
                {
                    _chunks.Add(_arrayPool.Rent(_chunkSize));
                }
                byte[] currentChunk = _chunks[currentChunkIndex];

                int bytesToWriteInChunk = Math.Min(count, _chunkSize - currentChunkOffset);

                Buffer.BlockCopy(buffer, offset, currentChunk, currentChunkOffset, bytesToWriteInChunk);

                _position += bytesToWriteInChunk;
                offset += bytesToWriteInChunk;
                count -= bytesToWriteInChunk;
                _length = Math.Max(_length, _position);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = offset;
                    break;
                case SeekOrigin.Current:
                    _position += offset;
                    break;
                case SeekOrigin.End:
                    _position = _length + offset;
                    break;
            }
            return _position;
        }

        public override void SetLength(long value)
        {
            // This implementation doesn't support arbitrary length setting beyond current chunks
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var chunk in _chunks)
                {
                    _arrayPool.Return(chunk);
                }
                _chunks.Clear();
            }
            base.Dispose(disposing);
        }
    }
}

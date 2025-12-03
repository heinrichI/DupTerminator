using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic
{
    //public sealed class PooledMemoryStream : MemoryStream
    public sealed class PooledMemoryStream : Stream
    {
        //private readonly byte[] _rentedBuffer;

        //public PooledMemoryStream(int capacity)
        //{
        //    if (capacity < 0)
        //        throw new ArgumentOutOfRangeException(nameof(capacity));

        //    _rentedBuffer = ArrayPool<byte>.Shared.Rent(capacity);
        //    // Backs stream with rented buffer (non-resizable, Length=0 initially)
        //    base.Write(_rentedBuffer, 0, 0);  // Dummy write to initialize safely (avoids issues)
        //    Capacity = capacity;
        //}

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing && _rentedBuffer != null)
        //    {
        //        // Clear for security (optional, perf hit; false for speed)
        //        ArrayPool<byte>.Shared.Return(_rentedBuffer, clearArray: true);
        //    }
        //    base.Dispose(disposing);
        //}

        private readonly MemoryPool<byte> _pool;
        private readonly IMemoryOwner<byte> _owner;
        private readonly Memory<byte> _memory;
        private int _position;

        public PooledMemoryStream(int capacity)
        {
            _pool = MemoryPool<byte>.Shared;
            _owner = _pool.Rent(capacity);
            _memory = _owner.Memory.Slice(0, capacity);
            _position = 0;
        }

        public PooledMemoryStream(IMemoryOwner<byte> owner, int capacity)
        {
            _pool = MemoryPool<byte>.Shared;
            _owner = owner;
            _memory = _owner.Memory.Slice(0, capacity);
            _position = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => _memory.Length;

        public override long Position
        {
            get => _position;
            set => _position = (int)value;
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position + count > _memory.Length)
                count = _memory.Length - _position;
            var span = _memory.Span.Slice(_position, count);
            span.CopyTo(buffer.AsSpan(offset));
            _position += count;
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = (int)offset;
                    break;
                case SeekOrigin.Current:
                    _position += (int)offset;
                    break;
                case SeekOrigin.End:
                    _position = _memory.Length + (int)offset;
                    break;
            }
            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var span = _memory.Span.Slice(_position, count);
            buffer.AsSpan(offset, count).CopyTo(span);
            _position += count;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _owner?.Dispose();  // Return buffer to pool
        }
    }
}

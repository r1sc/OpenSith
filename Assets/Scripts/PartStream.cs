using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    class PartStream : Stream
    {
        private Stream _stream;
        private readonly int _length;
        private long _start;

        public long End
        {
            get { return _start + _length; }
        }

        public PartStream(Stream stream, int length)
        {
            _stream = stream;
            _start = _stream.Position;
            _length = length;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long targetPos = offset;
            switch (origin)
            {
                case SeekOrigin.Current:
                    targetPos = _stream.Position + offset;
                    break;
                case SeekOrigin.End:
                    targetPos = End - offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("origin");
            }
            if (offset >= End || offset < 0)
                throw new InvalidOperationException("Attempt to seek beyond length of stream");
            _stream.Position = _start + targetPos;
            return _stream.Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = (int)Math.Min(count, Length - Position);
            var readBytes = _stream.Read(buffer, offset, count);
            Position = _stream.Position - _start;
            return readBytes;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _length; }
        }

        public override long Position { get; set; }
    }
}

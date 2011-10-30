using System;
using System.IO;

namespace Epsilon.IO.Compression.Cabinet
{
    /// <summary>
    /// Wraps a stream at any arbitrary position to allow absolute addressing.
    /// </summary>
    public class StreamWrapper : Stream
    {
        readonly Stream _baseStream;
        readonly long _initialPosition;

        public Stream BaseStream
        {
            get { return this._baseStream; }
        }

        public StreamWrapper(Stream stream)
        {
            this._baseStream = stream;
            this._initialPosition = this._baseStream.Position;
        }

        public override bool CanRead
        {
            get { return this._baseStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return this._baseStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return this._baseStream.CanWrite; }
        }

        public override void Flush()
        {
            this._baseStream.Flush();
        }

        public override long Length
        {
            get { return this._baseStream.Length - this._initialPosition; }
        }

        public override long Position
        {
            get
            {
                return this._baseStream.Position - this._initialPosition;
            }
            set
            {
                this._baseStream.Position = value + this._initialPosition;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this._baseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition = this.Position;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;

                case SeekOrigin.Current:
                    newPosition += offset;
                    break;

                case SeekOrigin.End:
                    newPosition = this.Length - 1 - offset;
                    break;
            }
            this.Position = newPosition;
            return newPosition;
        }

        public override void SetLength(long value)
        {
            this._baseStream.SetLength(value + this._initialPosition);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this._baseStream.Write(buffer, offset, count);
        }

        public override void Close()
        {
            this._baseStream.Close();
        }
    }
}

// Copyright © 2010 "Da_FileServer"
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;

namespace Nexon
{
    internal sealed class BoundedStream : Stream
    {
        #region Fields

        private Stream baseStream;
        private long baseOffset;
        private long baseLength;
        private long position;

        #endregion

        #region Constructors

        public BoundedStream(Stream stream, long offset, long length)
        {
            System.Diagnostics.Debug.Assert(stream != null);

            this.baseStream = stream;
            this.baseOffset = offset;
            this.baseLength = length;
            this.position = this.baseStream.Position - this.baseOffset;
            if (this.position < 0)
            {
                this.baseStream.Seek(-this.position, SeekOrigin.Current);
                this.position = 0;
            }
        }

        #endregion

        #region Properties

        public override bool CanRead
        {
            get { return this.baseStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return this.baseStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return this.baseStream.CanWrite; }
        }

        public override long Length
        {
            get { return this.baseLength; }
        }

        public override long Position
        {
            get { return this.position; }
            set
            {
                if (!this.CanSeek)
                    throw new NotSupportedException("Cannot seek in stream.");
                if (value < 0 || value > this.baseLength)
                    throw new ArgumentOutOfRangeException("value");
                this.baseStream.Position = this.baseOffset + value;
                this.position = this.baseStream.Position - this.baseOffset;
                System.Diagnostics.Debug.Assert(this.position >= 0 && this.position <= this.baseLength);
            }
        }

        #endregion

        #region Methods

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                this.baseStream.Dispose();
        }

        public override void Flush()
        {
            this.baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || (offset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("count");
            // Make sure they are reading within the bounds.
            if ((this.position + count) > this.baseLength)
                count = Convert.ToInt32(Math.Min(this.baseLength - this.position, int.MaxValue));
            int bytesRead = this.baseStream.Read(buffer, offset, count);
            this.position += bytesRead;
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!this.CanSeek)
                throw new NotSupportedException("Cannot seek in stream.");
            long temp;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0 || offset > this.baseLength)
                        throw new ArgumentOutOfRangeException("offset");
                    temp = this.baseStream.Seek(this.baseOffset + offset, SeekOrigin.Begin);
                    System.Diagnostics.Debug.Assert(temp == (this.baseOffset + offset));
                    break;
                case SeekOrigin.Current:
                    temp = this.position + offset;
                    if (temp < 0 || temp > this.baseLength)
                        throw new ArgumentOutOfRangeException("offset");
                    temp = this.baseStream.Seek(offset, SeekOrigin.Current);
                    System.Diagnostics.Debug.Assert(temp == (this.baseOffset + this.position + offset));
                    break;
                case SeekOrigin.End:
                    temp = this.baseLength + offset;
                    if (temp < 0 || temp > this.baseLength)
                        throw new ArgumentOutOfRangeException("offset");
                    temp = this.baseStream.Seek(offset, SeekOrigin.End);
                    System.Diagnostics.Debug.Assert(temp == (this.baseOffset + this.baseLength + offset));
                    break;
                default:
                    throw new ArgumentException("Not a valid seek origin.", "origin");
            }
            this.position = temp - this.baseOffset;
            System.Diagnostics.Debug.Assert(this.position >= 0 && this.position <= this.baseLength);
            return this.position;
        }

        public override void SetLength(long value)
        {
            if (value < (this.baseOffset + this.baseLength))
                throw new ArgumentException("Value is less than the stream's boundaries.", "value");
            this.baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || (offset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("count");
            // Make sure they are writing within the bounds.
            if ((this.position + count) > this.baseLength)
                throw new ArgumentOutOfRangeException("count");
            this.baseStream.Write(buffer, offset, count);
            this.position += count;
        }

        #endregion
    }
}

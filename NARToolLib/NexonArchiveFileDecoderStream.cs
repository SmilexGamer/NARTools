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
using System.Text;

namespace Nexon
{
    public sealed class NexonArchiveFileDecoderStream : Stream
    {
        #region Fields

        private const int KeySize = 16;

        private bool leaveOpen;
        private bool encode;
        private Stream baseStream;
        private byte[] key = new byte[KeySize];

        #endregion

        #region Constructors

        public NexonArchiveFileDecoderStream(Stream stream, string path, bool encode)
            : this(stream, path, encode, false)
        {
        }

        public NexonArchiveFileDecoderStream(Stream stream, string path, bool encode, bool leaveOpen)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (path == null)
                throw new ArgumentNullException("path");

            if (encode)
            {
                if (!stream.CanWrite)
                    throw new ArgumentException("Cannot write to stream.");
            }
            else
            {
                if (!stream.CanRead)
                    throw new ArgumentException("Cannot read from stream.");
            }

            this.encode = encode;
            this.baseStream = stream;
            this.leaveOpen = leaveOpen;

            this.GenerateKey(path);
        }

        #endregion

        #region Properties

        public Stream BaseStream
        {
            get { return this.baseStream; }
        }

        public override bool CanRead
        {
            get { return !this.encode; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return this.encode; }
        }

        public override long Length
        {
            get { return this.baseStream.Length; }
        }

        public override long Position
        {
            get { return this.baseStream.Position; }
            set { this.baseStream.Position = value; }
        }

        #endregion

        #region Methods

        private static uint PythonHash(byte[] data)
        {
            System.Diagnostics.Debug.Assert(data != null);

            uint hash = 0;
            for (int i = 0; i < data.Length; ++i)
                hash = unchecked(hash * 1000003) ^ data[i];
            return hash ^ (uint)data.Length;
        }

        private void GenerateKey(string path)
        {
            System.Diagnostics.Debug.Assert(path != null);

            uint seed = PythonHash(Encoding.ASCII.GetBytes(path));
            // This is a simple random number generator.
            for (int i = 0; i < KeySize; ++i)
            {
                seed = unchecked((seed * 1103515245) + 12345);
                this.key[i] = (byte)(seed & 255);
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && !this.leaveOpen)
                {
                    this.baseStream.Close();
                }
            }
            finally
            {
                this.baseStream = null;
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            if (this.baseStream == null)
                throw new ObjectDisposedException(null, "The stream has been disposed.");

            if (this.encode)
            {
                this.baseStream.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.baseStream == null)
                throw new ObjectDisposedException(null, "The stream has been disposed.");
            if (this.encode)
                throw new NotSupportedException("Cannot read from stream.");

            long tempOffset = this.Position;
            int length = this.baseStream.Read(buffer, offset, count);
            System.Diagnostics.Debug.Assert(length >= 0 && length <= count);
            for (int i = 0; i < length; ++i)
                buffer[offset + i] ^= this.key[(tempOffset + i) & 15];
            return length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (this.baseStream == null)
                throw new ObjectDisposedException(null, "The stream has been disposed.");

            return this.baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("value");
            if (this.baseStream == null)
                throw new ObjectDisposedException(null, "The stream has been disposed.");
            if (!this.encode)
                throw new NotSupportedException("Cannot write to stream.");

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
            if (this.baseStream == null)
                throw new ObjectDisposedException(null, "The stream has been disposed.");
            if (!this.encode)
                throw new NotSupportedException("Cannot write to stream.");

            // Note: this implementation will modify the buffer!

            long tempOffset = this.Position;
            for (int i = 0; i < count; ++i)
                buffer[offset + i] ^= this.key[(tempOffset + i) & 15];

            this.baseStream.Write(buffer, offset, count);
        }

        #endregion
    }
}

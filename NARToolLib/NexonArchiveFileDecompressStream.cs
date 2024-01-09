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
    public sealed class NexonArchiveFileDecompressStream : Stream
    {
        #region Fields

        /// <summary>
        /// The maximum number of bytes that a dictionary lookup can go.
        /// </summary>
        private const int DictionaryMaxSize = 8192;

        private bool leaveOpen;
        private Stream baseStream;

        // This could also be called the "window"... but dictionary is probably
        // more accurate.
        CircularBuffer dictionary = new CircularBuffer(DictionaryMaxSize);
        int lastReadDistance;
        int lastReadLength;

        #endregion

        #region Constructors

        public NexonArchiveFileDecompressStream(Stream stream)
            : this(stream, false)
        {
        }

        public NexonArchiveFileDecompressStream(Stream stream, bool leaveOpen)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanRead)
                throw new ArgumentException("Cannot read from stream.", "stream");

            this.baseStream = stream;
            this.leaveOpen = leaveOpen;
        }

        #endregion

        #region Properties

        public Stream BaseStream
        {
            get { return this.baseStream; }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        #endregion

        #region Methods

        public override void Flush()
        {
            if (this.baseStream == null)
                throw new ObjectDisposedException(null, "The stream has been disposed.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || (offset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("count");
            if (this.baseStream == null)
                throw new ObjectDisposedException(null, "The stream has been disposed.");

            // Enumerate until there is no more buffer left.
            int totalCountFixed = count;
            while (count > 0)
            {
                if (this.lastReadLength == 0)
                {
                    // If the header is not available, then the end of the
                    // stream has been reached.
                    if (!this.ReadHeader())
                        break;

                    System.Diagnostics.Debug.Assert(this.lastReadDistance >= 0);
                    System.Diagnostics.Debug.Assert(this.lastReadLength > 0);
                    if (this.lastReadDistance > this.dictionary.Length)
                        throw new InvalidDataException("Distance is greater than the dictionary's current length.");
                }

                int tempCount = Math.Min(count, this.lastReadLength);
                if (this.lastReadDistance == 0)
                {
                    // Read raw bytes from current source to current destination. (1..32 bytes from source)

                    int lengthRead = this.baseStream.Read(buffer, offset, tempCount);
                    System.Diagnostics.Debug.Assert(lengthRead > 0 && lengthRead <= tempCount);
                    if (lengthRead == 0)
                        throw new InvalidDataException("Expected " + this.lastReadLength + " more bytes in compressed stream.");

                    this.dictionary.Append(buffer, offset, lengthRead);
                    this.lastReadLength -= lengthRead;
                    //this.outputPosition += lengthRead;
                    count -= lengthRead;
                    offset += lengthRead;
                }
                else
                {
                    // Read bytes from previous destination (dictionary) to current destination. (0 bytes from source)

                    this.dictionary.CopyAndUpdate(this.lastReadDistance, buffer, offset, tempCount);
                    this.lastReadLength -= tempCount;
                    //this.outputPosition += tempCount;
                    count -= tempCount;
                    offset += tempCount;
                }

                // Do it one command at a time...
                break;
            }

            System.Diagnostics.Debug.Assert(count >= 0);
            return totalCountFixed - count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Cannot seek in stream.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Cannot write to stream.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Cannot write to stream.");
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && this.baseStream != null && !this.leaveOpen)
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

        private byte ReadByteChecked()
        {
            int temp = this.baseStream.ReadByte();
            if (temp < 0)
                throw new EndOfStreamException();
            return Convert.ToByte(temp);
        }

        private bool ReadHeader()
        {
            // Read the operation and length byte. (1 byte from source)
            int tempByte = this.baseStream.ReadByte();
            if (tempByte < 0)
                return false;

            System.Diagnostics.Debug.Assert(tempByte <= byte.MaxValue);
            int operation = tempByte >> 5;
            int length = tempByte & 31;

            if (operation == 0)
            {
                // Read raw bytes from current source to current destination. (0 bytes from source)

                this.lastReadDistance = 0;
                this.lastReadLength = length + 1; // min=1 max=32
            }
            else
            {
                // Read bytes from previous destination to current destination. (1-2 bytes from source)

                if (operation == 7)
                    operation += this.ReadByteChecked();
                operation += 2;

                length = ((length << 8) | this.ReadByteChecked()) + 1;

                this.lastReadDistance = length; // min=1 max=8192
                this.lastReadLength = operation; // min=3 max=264
            }

            return true;
        }

        #endregion
    }
}

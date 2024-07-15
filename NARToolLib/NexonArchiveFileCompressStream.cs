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
    public sealed class NexonArchiveFileCompressStream : Stream
    {
        #region Fields

        /// <summary>
        /// The maximum number of bytes which can be stored in a single raw data
        /// operation.
        /// </summary>
        private const int RawBufferMaxSize = 32;

        /// <summary>
        /// The maximum number of bytes that a dictionary lookup can go.
        /// </summary>
        private const int DictionaryMaxSize = 8192;

        /// <summary>
        /// The maximum number of bytes in a dictionary lookup.
        /// </summary>
        private const int ChainBufferMaxSize = 264;

        private bool leaveOpen;
        private Stream baseStream;

        private byte[] rawBuffer = new byte[RawBufferMaxSize];
        private int rawBufferLength;

        private CircularBuffer dictionary = new CircularBuffer(DictionaryMaxSize);

        private byte[] chainBuffer = new byte[ChainBufferMaxSize];
        private int chainBufferLength;

        #endregion

        #region Constructors

        public NexonArchiveFileCompressStream(Stream stream)
            : this(stream, false)
        {
        }

        public NexonArchiveFileCompressStream(Stream stream, bool leaveOpen)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanWrite)
                throw new ArgumentException("Cannot write to stream.", "stream");

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
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
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

            this.baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Cannot read from stream.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Cannot seek in stream.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Cannot set stream length.");
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

            // Add the buffer data to the chain.
            while (count > 0)
            {
                System.Diagnostics.Debug.Assert(this.chainBufferLength >= 0);
                System.Diagnostics.Debug.Assert(this.chainBufferLength <= ChainBufferMaxSize);

                // When the chain buffer is full, process a single operation.
                int chainBufferRemaining = ChainBufferMaxSize - this.chainBufferLength;
                if (chainBufferRemaining == 0)
                {
                    this.ProcessChainBuffer();
                    continue;
                }

                // Try to fill the chain buffer.
                int copyCount = Math.Min(count, chainBufferRemaining);
                Buffer.BlockCopy(buffer, offset, this.chainBuffer, this.chainBufferLength, copyCount);
                count -= copyCount;
                offset += copyCount;
                this.chainBufferLength += copyCount;
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && this.baseStream != null)
                {
                    // Finish processing any remaining data.
                    while (this.chainBufferLength > 0)
                    {
                        this.ProcessChainBuffer();
                    }
                    this.FlushRawBuffer();
                }
            }
            finally
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
        }

        private void FlushRawBuffer()
        {
            System.Diagnostics.Debug.Assert(this.rawBufferLength >= 0);
            if (this.rawBufferLength > 0)
            {
                System.Diagnostics.Debug.Assert(this.rawBufferLength <= RawBufferMaxSize);
                // Note: a possible (small) optimization could be done by
                // reserving index zero of the raw buffer for this byte and
                // adding one to the length when it's created.
                this.baseStream.WriteByte((byte)(this.rawBufferLength - 1));
                this.baseStream.Write(this.rawBuffer, 0, this.rawBufferLength);
                this.rawBufferLength = 0;
            }
        }

        private void AddRawByte(byte value)
        {
            System.Diagnostics.Debug.Assert(this.rawBufferLength >= 0);
            System.Diagnostics.Debug.Assert(this.rawBufferLength < RawBufferMaxSize);
            this.rawBuffer[this.rawBufferLength++] = value;
            if (this.rawBufferLength == RawBufferMaxSize)
            {
                this.FlushRawBuffer();
                this.rawBufferLength = 0;
            }
        }

        private void ProcessChainBuffer()
        {
            // Try to find the longest match in the dictionary.
            int matchDistance;
            int matchLength = this.dictionary.FindLongestMatch(this.chainBuffer, 0, this.chainBufferLength, out matchDistance);

            // Only matches with 3 or more bytes in them can be used as valid
            // dictionary references.
            if (matchLength >= 3)
            {
                System.Diagnostics.Debug.Assert(matchDistance > 0);
                System.Diagnostics.Debug.Assert(matchLength <= ChainBufferMaxSize);

                // Flush any raw bytes currently buffered.
                this.FlushRawBuffer();

                // Update the dictionary and chain buffer before messing with
                // the values.
                this.dictionary.Append(this.chainBuffer, 0, matchLength);
                this.chainBufferLength -= matchLength;
                if (this.chainBufferLength > 0)
                {
                    Buffer.BlockCopy(this.chainBuffer, matchLength, this.chainBuffer, 0, this.chainBufferLength);
                }

                --matchDistance;

                matchLength -= 2;

                // Handle extended lengths.
                byte extendedLength;
                bool hasExtendedLength;
                if (matchLength >= 7)
                {
                    extendedLength = (byte)(matchLength - 7);
                    matchLength = 7;
                    hasExtendedLength = true;
                }
                else
                {
                    extendedLength = 0;
                    hasExtendedLength = false;
                }

                // Write the operation byte.
                System.Diagnostics.Debug.Assert(matchDistance < DictionaryMaxSize);
                System.Diagnostics.Debug.Assert(matchLength < 8);
                this.baseStream.WriteByte((byte)((matchLength << 5) | (matchDistance >> 8)));

                // Write the extended length byte.
                if (hasExtendedLength)
                    this.baseStream.WriteByte(extendedLength);

                // Write the extended distance byte.
                this.baseStream.WriteByte((byte)(matchDistance & 255));
            }
            else
            {
                // An acceptable dictionary match was not found, so add the
                // first byte from the chain buffer to the raw byte data.
                byte raw = this.chainBuffer[0];
                this.AddRawByte(raw);
                this.dictionary.Append(raw);
                --this.chainBufferLength;
                Buffer.BlockCopy(this.chainBuffer, 1, this.chainBuffer, 0, this.chainBufferLength);
            }
        }

        #endregion
    }
}

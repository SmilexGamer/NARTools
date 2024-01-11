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

using ICSharpCode.SharpZipLib.Checksums;

namespace Nexon
{
    public sealed class NexonArchiveFileEntry
    {
        #region Fields

        private NexonArchive archive;
        private string path;
        private NexonArchiveFileEntryType storedType;
        private long offset;
        private long storedSize;
        private long extractedSize;
        private DateTime lastModifiedTime;
        private uint checksum;

        #endregion

        #region Constructors

        internal NexonArchiveFileEntry(NexonArchive archive)
        {
            System.Diagnostics.Debug.Assert(archive != null);

            this.archive = archive;
        }

        #endregion

        #region Properties

        public NexonArchive Archive
        {
            get { return this.archive; }
        }

        public string Path
        {
            get { return this.path; }
        }

        public NexonArchiveFileEntryType StoredType
        {
            get { return this.storedType; }
        }

        public long CompressedSize
        {
            get { return this.storedSize; }
        }

        public long Size
        {
            get { return this.extractedSize; }
        }

        public DateTime LastModifiedTime
        {
            get { return this.lastModifiedTime; }
        }

        public long Checksum
        {
            get { return this.checksum; }
        }

        #endregion

        #region Methods

        private static DateTime FromEpoch(int epoch)
        {
            return new DateTime((epoch * TimeSpan.TicksPerSecond) + 621355968000000000);
        }

        //private static int ToEpoch(DateTime date)
        //{
        //    return Convert.ToInt32((date.Ticks - 621355968000000000) / TimeSpan.TicksPerSecond);
        //}

        internal int Load(byte[] header, int offset)
        {
            System.Diagnostics.Debug.Assert(header != null);
            System.Diagnostics.Debug.Assert(offset >= 0 && offset < header.Length);

            try
            {
                int pathSize = BitConverter.ToUInt16(header, offset);
                this.path = Encoding.GetEncoding("euc-kr").GetString(header, offset + 2, pathSize);
                this.storedType = (NexonArchiveFileEntryType)BitConverter.ToInt32(header, offset + 2 + pathSize);
                this.offset = BitConverter.ToUInt32(header, offset + 2 + pathSize + 4);
                this.storedSize = BitConverter.ToInt32(header, offset + 2 + pathSize + 8);
                this.extractedSize = BitConverter.ToInt32(header, offset + 2 + pathSize + 12);
                this.lastModifiedTime = FromEpoch(BitConverter.ToInt32(header, offset + 2 + pathSize + 16));
                this.checksum = BitConverter.ToUInt32(header, offset + 2 + pathSize + 20);

                System.Diagnostics.Debug.Assert(Enum.IsDefined(typeof(NexonArchiveFileEntryType), this.storedType));

                return 2 + pathSize + 24;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new InvalidDataException("NAR file entry is invalid.", ex);
            }
        }

        public long Extract(Stream outputStream)
        {
            if (outputStream == null)
                throw new ArgumentNullException("outputStream");
            if (!outputStream.CanWrite)
                throw new ArgumentException("Cannot write to stream.", "outputStream");

            // Shortcut if the file is zero bytes in length.
            if (this.extractedSize == 0)
                return 0;

            // Lock the file stream to make sure it doesn't get desynchronized.
            lock (this.archive.Stream)
            {
                Stream readStream = new BoundedStream(this.archive.Stream, this.offset, this.storedSize);
                readStream.Position = 0;
                switch (this.storedType)
                {
                    case NexonArchiveFileEntryType.Raw:
                        break;
                    case NexonArchiveFileEntryType.Encoded:
                        readStream = new NexonArchiveFileDecoderStream(readStream, this.path, false);
                        break;
                    case NexonArchiveFileEntryType.EncodedAndCompressed:
                        readStream = new NexonArchiveFileDecompressStream(
                            new NexonArchiveFileDecoderStream(readStream, this.path, false), this.extractedSize);
                        break;
                    default:
                        throw new NotSupportedException("Unsupported file storage type: " + this.storedType + ".");
                }

                System.Diagnostics.Debug.Assert(readStream != null);

                // Lock the output stream to make sure it doesn't get
                // desynchronized.
                lock (outputStream)
                {
                    System.Diagnostics.Debug.Assert(readStream.Length == this.extractedSize);

                    // Extract data and store into output stream.
                    byte[] buffer = new byte[8192];
                    int length;
                    long totalLength = 0;
                    while ((length = readStream.Read(buffer, 0, 8192)) > 0)
                    {
                        outputStream.Write(buffer, 0, length);
                        totalLength += length;
                    }
                    System.Diagnostics.Debug.Assert(totalLength == this.extractedSize);
                    return totalLength;
                }
            }
        }

        public bool Verify()
        {
            Crc32 crc = new Crc32();
            // Lock the file stream to make sure it doesn't get desynchronized.
            lock (this.archive.Stream)
            {
                Stream readStream = new BoundedStream(this.archive.Stream, this.offset, this.storedSize);
                readStream.Position = 0;

                byte[] buffer = new byte[8192];
                int length;
                while ((length = readStream.Read(buffer, 0, 8192)) > 0)
                {
                    crc.Update(buffer, 0, length);
                }
            }
            return this.checksum == crc.Value;
        }

        #endregion
    }
}

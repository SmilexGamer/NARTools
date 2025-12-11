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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using ICSharpCode.SharpZipLib.BZip2;

namespace Nexon
{
    public sealed class NexonArchive : IDisposable
    {
        #region Fields

        private static readonly byte[] HeaderXor = {
            0x19, 0x5B, 0x7B, 0x2C, 0x65, 0x5E, 0x79, 0x25,
            0x6E, 0x4B, 0x07, 0x21, 0x62, 0x7F, 0x00, 0x29
        };

        private Stream stream;
        private List<NexonArchiveFileEntry> fileEntries = new List<NexonArchiveFileEntry>();

        #endregion

        #region Constructors

        public NexonArchive()
        {
        }

        #endregion

        #region Properties

        internal Stream Stream
        {
            get { return this.stream; }
        }

        public ReadOnlyCollection<NexonArchiveFileEntry> FileEntries
        {
            get { return this.fileEntries.AsReadOnly(); }
        }

        #endregion

        #region Methods

        public void Load(string fileName, bool writable)
        {
            if (this.stream != null)
                throw new InvalidOperationException("The archive must be disposed before it can be loaded again.");

            Load(new FileStream(fileName, FileMode.Open, writable ? FileAccess.ReadWrite : FileAccess.Read, FileShare.Read), writable);
        }

        public void Load(Stream stream, bool writable)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanRead)
                throw new ArgumentException("Cannot read from stream.", "stream");
            if (!stream.CanSeek)
                throw new ArgumentException("Cannot seek in stream.", "stream");
            if (writable && !stream.CanWrite)
                throw new ArgumentException("Cannot write to stream.", "stream");

            if (this.stream != null)
                throw new InvalidOperationException("The archive must be disposed before it can be loaded again.");

            int headerSize;
            byte[] header;

            lock (stream)
            {
                // Set the position to zero, if it isn't already.
                stream.Position = 0;

                this.stream = stream;

                BinaryReader reader = new BinaryReader(this.stream);

                if (reader.ReadInt32() != 0x0052414E)
                    throw new InvalidDataException("NAR file signature is invalid.");

                if (reader.ReadInt32() != 0x01000000)
                    throw new InvalidDataException("NAR file version is invalid.");

                if (this.stream.Length < 16)
                    throw new InvalidDataException("NAR file is not long enough to be valid.");

                this.stream.Seek(-4, SeekOrigin.End);

                if (reader.ReadInt32() != 0x0052414E)
                    throw new InvalidDataException("NAR end file signature is invalid.");

                this.stream.Seek(-8, SeekOrigin.Current);

                headerSize = reader.ReadInt32() ^ 0x4076551F;

                if (this.stream.Length < (headerSize + 16))
                    throw new InvalidDataException("NAR file is not long enough to be valid.");

                this.stream.Seek(-4 - headerSize, SeekOrigin.Current);
                header = reader.ReadBytes(headerSize);
                System.Diagnostics.Debug.Assert((this.stream.Position + 8) == this.stream.Length);
            }

            for (int i = 0; i < header.Length; ++i)
                header[i] ^= HeaderXor[i & 15];

            using (MemoryStream decompressedHeaderStream = new MemoryStream(headerSize))
            {
                BZip2.Decompress(new MemoryStream(header, false), decompressedHeaderStream);

                byte[] decompressedHeader = decompressedHeaderStream.ToArray();
                LoadHeader(decompressedHeader);
            }
        }

        private void LoadHeader(byte[] header)
        {
            System.Diagnostics.Debug.Assert(header != null);

            if (header.Length < 4)
                throw new InvalidDataException("NAR header is invalid.");

            // Get the header version.
            int version = BitConverter.ToInt32(header, 0);
            if (version == 1 || version == 2)
            {
                if (header.Length < 16)
                    throw new InvalidDataException("NAR header is invalid.");

                int unk = BitConverter.ToInt32(header, 4); // always 0
                int directorySize = BitConverter.ToInt32(header, 8);
                int unk2 = BitConverter.ToInt32(header, 12); // always 1

                System.Diagnostics.Debug.Assert(directorySize >= 0);

                int directoryCount = BitConverter.ToInt32(header, 16);
                if (directoryCount < 0)
                    throw new InvalidDataException("Directory entry count is too large.");
                int entryOffset = 20;
                for (int i = 0; i < directoryCount; i++)
                {
                    System.Diagnostics.Debug.Assert((entryOffset + 26) <= header.Length);
                    NexonArchiveFileEntry fileEntry = new NexonArchiveFileEntry(this);
                    entryOffset += fileEntry.Load(header, entryOffset);
                    System.Diagnostics.Debug.Assert(entryOffset <= header.Length);
                    this.fileEntries.Add(fileEntry);
                }

                if (version == 2)
                {
                    System.Diagnostics.Debug.Assert((entryOffset + 24) <= header.Length);
                    int unk3 = BitConverter.ToInt32(header, entryOffset); // always 1
                    int size = BitConverter.ToInt32(header, entryOffset + 4); // size for more data
                    System.Diagnostics.Debug.Assert((entryOffset + size + 12) == header.Length);
                    int unk4 = BitConverter.ToInt32(header, entryOffset + 8); // always 0
                    uint oldchecksum = BitConverter.ToUInt32(header, entryOffset + 12); // old checksum (from NAP file)
                    uint newchecksum = BitConverter.ToUInt32(header, entryOffset + 16); // new checksum (from NAR file after patch)
                    int moreData = BitConverter.ToInt32(header, entryOffset + 20); // more data
                    entryOffset += 24;

                    if (moreData == 1)
                    {
                        System.Diagnostics.Debug.Assert((entryOffset + size - 12) <= header.Length);
                        List<byte> unk5 = new List<byte>();
                        for (int i = 0; i < size - 12; i++)
                            unk5.Add(header[entryOffset + i]);

                        entryOffset += size - 12;
                    }
                }

                System.Diagnostics.Debug.Assert(entryOffset == header.Length);
            }
            else
                throw new InvalidDataException("NAR header version is invalid.");
        }

        public void Close()
        {
            this.fileEntries.Clear();
            stream.Close();
            stream = null;
        }

        public void Dispose()
        {
            Close();
        }

        #endregion
    }
}

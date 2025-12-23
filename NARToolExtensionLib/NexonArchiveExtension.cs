using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Nexon.Extension
{
    public sealed class NexonArchiveExtension : IDisposable
    {
        /// <summary>
        /// The archive header & footer signature.
        /// </summary>
        public static readonly int SIGNATURE = 0x0052414E;

        /// <summary>
        /// The archive version.
        /// </summary>
        public static readonly int VERSION = 0x01000000;

        /// <summary>
        /// The temporary file path for compression processing.
        /// </summary>
        public static readonly string PATH_TEMP_COMPRESSION = Path.GetTempPath() + "/" + Path.GetRandomFileName() + "_repack_compressed.tmp";

        /// <summary>
        /// File entry xor sequences.
        /// </summary>
        private static readonly byte[] HeaderXor = {
            0x19, 0x5B, 0x7B, 0x2C, 0x65, 0x5E, 0x79, 0x25,
            0x6E, 0x4B, 0x07, 0x21, 0x62, 0x7F, 0x00, 0x29
        };


        private List<NexonArchiveExtensionFileEntry> fileEntries = new List<NexonArchiveExtensionFileEntry>();
        /// <summary>
        /// The file entries.
        /// </summary>
        public ReadOnlyCollection<NexonArchiveExtensionFileEntry> FileEntries
        {
            get
            {
                return this.fileEntries.AsReadOnly();
            }
        }


        /// <summary>
        /// Temporary file stream.
        /// </summary>
        public FileStream TempFile { get; set; }


        /// <summary>
        /// The output file path.
        /// </summary>
        public string OutputFilePath { get; set; }


        /// <summary>
        /// Initializes a new instance of the Archive class.
        /// </summary>
        /// <param name="path">The output file path.</param>
        public NexonArchiveExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path is empty string.", nameof(path));

            OutputFilePath = path;
            CreateTempFile();
        }


        /// <summary>
        /// Insert a file into archive.
        /// Returns the created file entry.
        /// </summary>
        /// <param name="inStream">The readable stream.</param>
        /// <param name="path">The absolute file entry path started from archive root directory.</param>
        /// <param name="storeType">The file store type.</param>
        /// <param name="lastModifiedTime">The last file modified/write time.</param>
        /// <param name="readChunkSize">The buffer read chunk size.</param>
        /// <returns></returns>
        public NexonArchiveExtensionFileEntry Add(Stream inStream, string path, NexonArchiveFileEntryType storeType, DateTime lastModifiedTime, NexonArchiveFileCompressionLevel compressionLevel, int readChunkSize = 8192)
        {
            if (inStream == null)
                throw new ArgumentNullException(nameof(inStream));
            if (!inStream.CanRead)
                throw new ArgumentException("Cannot read from stream.", nameof(inStream));
            if (!inStream.CanSeek)
                throw new ArgumentException("Cannot seek in stream.", nameof(inStream));

            long offset = TempFile.Length;
            long fileSize = inStream.Length;
            long storeSize = 0;
            var checksum = new ICSharpCode.SharpZipLib.Checksums.Crc32();
            var buffer = new byte[readChunkSize];

            switch (storeType)
            {
                case NexonArchiveFileEntryType.Raw:
                    break;
                case NexonArchiveFileEntryType.Encoded:
                    inStream = new NexonArchiveFileEncoderStream(inStream, path);
                    break;
                case NexonArchiveFileEntryType.EncodedAndCompressed:
                    int readLength;
                    using (var tempStreamIn = new FileStream(PATH_TEMP_COMPRESSION, FileMode.Create))
                    {
                        using (NexonArchiveFileCompressStream compressorStream = new NexonArchiveFileCompressStream(tempStreamIn, compressionLevel))
                        {
                            while ((readLength = inStream.Read(buffer, 0, readChunkSize)) > 0)
                            {
                                compressorStream.Write(buffer, 0, readLength);
                            }
                        }

                        inStream = new FileStream(tempStreamIn.Name, FileMode.Open);
                    }
                    goto case NexonArchiveFileEntryType.Encoded;
                default:
                    throw new NotSupportedException("Unsupported file storage type: " + (object)storeType + ".");
            }

            lock (inStream)
            {
                int count;
                while ((count = inStream.Read(buffer, 0, readChunkSize)) > 0 )
                {
                    checksum.Update(buffer, 0, count);
                    storeSize += Write(buffer, 0, count);
                }

                if ( storeType == NexonArchiveFileEntryType.EncodedAndCompressed )
                {
                    inStream.Close();
                }
            }

            var result = new NexonArchiveExtensionFileEntry(path, storeType, offset, storeSize, fileSize, lastModifiedTime, (uint)checksum.Value);
            fileEntries.Add( result );
            return result;
        }


        /// <summary>
        /// Save this archive into output file path.
        /// Returns the output file path.
        /// </summary>
        /// <param name="isOverwrite">Destination file will be overwritten if set to true.</param>
        /// <param name="isNAP">Writes archive as a NAP file if set to true.</param>
        /// <param name="prePatchCRC">The checksum of the NAR file before the patch.</param>
        /// <param name="postPatchCRC">The checksum of the NAR file after the patch.</param>
        public string Save( bool isOverwrite = true, bool isNAP = false, uint prePatchCRC = 0, uint postPatchCRC = 0 )
        {
            if (TempFile == null)
                throw new ArgumentNullException(nameof(TempFile));

            // Compress them.
            byte[] compressed = PackFileEntries(isNAP, prePatchCRC, postPatchCRC);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                ICSharpCode.SharpZipLib.BZip2.BZip2.Compress((Stream)new MemoryStream(compressed, false), (Stream)memoryStream, 1);
                compressed = memoryStream.ToArray();
            }

            // Encode them.
            byte[] encoded = compressed;
            for (int index = 0; index < encoded.Length; ++index)
                encoded[index] ^= HeaderXor[index & 15];

            Write(encoded);
            Write(GetFooter(compressed.Length));

            string tempPath = TempFile.Name;
            TempFile.Dispose();
            File.Move(tempPath, OutputFilePath, isOverwrite);

            return OutputFilePath;
        }

        public void RewritePatchCRCs(uint prePatchCRC, uint postPatchCRC)
        {
            if (OutputFilePath == null)
                throw new InvalidOperationException("Archive has not been saved yet.");

            using (FileStream fs = new FileStream(OutputFilePath, FileMode.Open, FileAccess.ReadWrite))
            using (BinaryReader reader = new BinaryReader(fs))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                fs.Seek(-8, SeekOrigin.End);
                int oldEncodedHeaderSize = reader.ReadInt32();
                int oldHeaderSize = oldEncodedHeaderSize ^ 0x4076551F;

                long headerOffset = fs.Length - 8 - oldHeaderSize;
                fs.Seek(headerOffset, SeekOrigin.Begin);

                byte[] encodedHeader = reader.ReadBytes(oldHeaderSize);

                for (int i = 0; i < encodedHeader.Length; i++)
                    encodedHeader[i] ^= HeaderXor[i & 15];

                byte[] header;
                using (var inMs = new MemoryStream(encodedHeader))
                using (var outMs = new MemoryStream())
                {
                    ICSharpCode.SharpZipLib.BZip2.BZip2.Decompress(inMs, outMs);
                    header = outMs.ToArray();
                }

                int version = BitConverter.ToInt32(header, 0);
                if (version != 2)
                    throw new InvalidOperationException("RewritePatchCRCs only applies to NAP (v2).");

                int directoryCount = BitConverter.ToInt32(header, 16);
                int entryOffset = 20;

                for (int i = 0; i < directoryCount; i++)
                {
                    ushort pathLength = BitConverter.ToUInt16(header, entryOffset);
                    entryOffset += 2;
                    entryOffset += pathLength;
                    entryOffset += 4; // storedType
                    entryOffset += 4; // offset
                    entryOffset += 4; // compressedSize
                    entryOffset += 4; // size
                    entryOffset += 4; // lastModifiedTime (uint32)
                    entryOffset += 4; // checksum
                }

                if (entryOffset + 20 > header.Length)
                    throw new InvalidDataException("NAP patch block is truncated.");

                Buffer.BlockCopy(BitConverter.GetBytes(prePatchCRC), 0, header, entryOffset + 12, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(postPatchCRC), 0, header, entryOffset + 16, 4);

                byte[] recompressed;
                using (var raw = new MemoryStream(header))
                using (var compressed = new MemoryStream())
                {
                    ICSharpCode.SharpZipLib.BZip2.BZip2.Compress(raw, compressed, 1);
                    recompressed = compressed.ToArray();
                }

                for (int i = 0; i < recompressed.Length; i++)
                    recompressed[i] ^= HeaderXor[i & 15];

                fs.SetLength(headerOffset);
                fs.Seek(headerOffset, SeekOrigin.Begin);

                writer.Write(recompressed);
                writer.Write(recompressed.Length ^ 0x4076551F);
                writer.Write(SIGNATURE);
            }
        }


        /// <param name="isNAP">Writes archive as a NAP file if set to true.</param>
        /// <param name="prePatchCRC">The checksum of the NAR file before the patch.</param>
        /// <param name="postPatchCRC">The checksum of the NAR file after the patch.</param>
        private byte[] PackFileEntries( bool isNAP = false, uint prePatchCRC = 0, uint postPatchCRC = 0 )
        {
            List<byte> result = new List<byte>();
            foreach (NexonArchiveExtensionFileEntry entry in fileEntries)
            {
                result.AddRange(entry.ToBytes());
            }
            result.InsertRange(0, NexonArchiveExtensionFileEntry.GetHeader(fileEntries.Count, result.Count + 4, isNAP));
            if (isNAP)
            {
                result.AddRange(NexonArchiveExtensionFileEntry.GetHeaderNAP(prePatchCRC, postPatchCRC));
            }
            return result.ToArray();
        }


        private void CreateTempFile()
        {
            if (TempFile != null)
                throw new InvalidOperationException("The archive must be disposed before it can be loaded again.");

            TempFile = new FileStream(OutputFilePath + "_temp", FileMode.Create);
            Write(GetHeader());
        }


        private int Write(byte[] buffer, bool leaveOpen = true)
        {
            return Write(buffer, 0, buffer.Length, leaveOpen );
        }


        private int Write(byte[] buffer, int index, int count, bool leaveOpen = true)
        {
            if (TempFile == null)
                throw new ArgumentNullException(nameof(TempFile));
            if (!TempFile.CanWrite)
                throw new ArgumentException("Cannot write to temp file stream.", nameof(TempFile));

            using (BinaryWriter writer = new BinaryWriter(TempFile, Encoding.UTF8, leaveOpen))
            {
                writer.Write(buffer, index, count);
            }

            return count;
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                Close();

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~NexonArchiveExtension()
        {
           // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
           Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion


        /// <summary>
        /// Close this archive and clear all resources.
        /// </summary>
        public void Close()
        {
            this.fileEntries.Clear();
            if (TempFile != null)
            {
                string tempPath = TempFile.Name;
                TempFile.Dispose();
                if (File.Exists(tempPath))
                    File.Delete(tempPath);

                if (File.Exists(PATH_TEMP_COMPRESSION))
                    File.Delete(PATH_TEMP_COMPRESSION);
            }
        }

        /// <summary>
        /// Returns a byte array represents archive header signature.
        /// </summary>
        /// <returns></returns>
        public static byte[] GetHeader()
        {
            List<byte> result = new List<byte>();
            result.AddRange(BitConverter.GetBytes( SIGNATURE ));    // offset 0; "NAR" format header signature
            result.AddRange(BitConverter.GetBytes( VERSION ));      // offset 4; format version
            return result.ToArray();
        }


        /// <summary>
        /// Returns a byte array represents archive footer signature.
        /// </summary>
        /// <returns></returns>
        public static byte[] GetFooter( int compressedEntriesSize )
        {
            List<byte> result = new List<byte>();
            result.AddRange(BitConverter.GetBytes(compressedEntriesSize ^ 0x4076551F)); // offset EoF-8; compressed file entries size.
            result.AddRange(BitConverter.GetBytes( SIGNATURE ));    // offset EoF-4; "NAR" format footer signature
            return result.ToArray();
        }

        // Add near fileEntries list
        public void FileEntriesInternalAdd(string path, NexonArchiveFileEntryType storeType, long offset, long storeSize, long fileSize, DateTime lastModifiedTime, uint checksum)
        {
            fileEntries.Add(new NexonArchiveExtensionFileEntry(path, storeType, offset, storeSize, fileSize, lastModifiedTime, checksum));
        }
    }
}

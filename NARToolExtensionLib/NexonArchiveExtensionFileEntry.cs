using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Nexon.Extension
{
    public sealed class NexonArchiveExtensionFileEntry
    {
        /// <summary>
        /// The file entry version.
        /// </summary>
        public static readonly int VERSION = 1;

        /// <summary>
        /// The absolute file path started from archive root directory.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// The store type.
        /// </summary>
        public NexonArchiveFileEntryType StoreType { get; private set; }

        /// <summary>
        /// The start data offset on archive.
        /// </summary>
        public long Offset { get; private set; }

        /// <summary>
        /// The stored data size.
        /// </summary>
        public long StoreSize { get; private set; }

        /// <summary>
        /// The original file size.
        /// </summary>
        public long Size { get; private set; }

        /// <summary>
        /// The last modified/write time.
        /// </summary>
        public DateTime LastModifiedTime { get; private set; }

        /// <summary>
        /// The stored data CRC32 checksum.
        /// </summary>
        public uint Checksum { get; private set; }


        /// <summary>
        /// Initializes a new instance of the FileEntry class.
        /// </summary>
        /// <param name="path">The absolute file path started from archive root directory.</param>
        /// <param name="storeType">The store type.</param>
        /// <param name="offset">The start offset on archive.</param>
        /// <param name="storeSize">The stored data size.</param>
        /// <param name="fileSize">The original file size.</param>
        /// <param name="lastModifiedTime">The last modified/write time.</param>
        /// <param name="checksum">The stored data CRC32 checksum.</param>
        public NexonArchiveExtensionFileEntry(string path, NexonArchiveFileEntryType storeType, long offset, long storeSize, long fileSize, DateTime lastModifiedTime, uint checksum)
        {
            Path = path;
            StoreType = storeType;
            Offset = offset;
            StoreSize = storeSize;
            Size = fileSize;
            LastModifiedTime = lastModifiedTime;
            Checksum = checksum;
        }


        /// <summary>
        /// Returns a byte array represents a single file entry.
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            byte[] pathEncoded = Encoding.GetEncoding("euc-kr").GetBytes(Path);
            List<byte> result = new List<byte>();
            result.AddRange(BitConverter.GetBytes((ushort)pathEncoded.Length));
            result.AddRange(pathEncoded);
            result.AddRange(BitConverter.GetBytes((int) StoreType));
            result.AddRange(BitConverter.GetBytes((int) Offset));
            result.AddRange(BitConverter.GetBytes((int) StoreSize));
            result.AddRange(BitConverter.GetBytes((int) Size));
            result.AddRange(BitConverter.GetBytes((int) new DateTimeOffset(LastModifiedTime).ToUnixTimeSeconds()));
            result.AddRange(BitConverter.GetBytes(Checksum));
            return result.ToArray();
        }


        /// <summary>
        /// Returns a byte array represents file entry header signature.
        /// </summary>
        /// <param name="counts">The file entry counts.</param>
        /// <returns></returns>
        public static byte[] GetHeader(int counts)
        {
            if (counts < 0)
                throw new InvalidDataException("Directory entry count is too large.");

            List<byte> result = new List<byte>();
            result.AddRange(BitConverter.GetBytes( VERSION ));  // offset 0; version
            result.AddRange(BitConverter.GetBytes((int) 0));    // offset 4; unknown
            result.AddRange(BitConverter.GetBytes((int) 4));    // offset 8; unknown
            result.AddRange(BitConverter.GetBytes((int) 1));    // offset 12; version?
            result.AddRange(BitConverter.GetBytes(counts));     // offset 16; entry counts
            return result.ToArray();
        }
    }
}

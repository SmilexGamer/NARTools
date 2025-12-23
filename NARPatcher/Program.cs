using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nexon.Extension;

namespace Nexon.Patcher
{
    class Program
    {
        private static NexonArchiveFileEntryType storeType = NexonArchiveFileEntryType.Encoded;
        private static NexonArchiveFileCompressionLevel compressionLevel = NexonArchiveFileCompressionLevel.Normal;

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: Patcher.exe <Base NAR> <Patch Folder | NAP File> [Store Type: 0 - Raw, 1 - Encoded, 2 - Encoded and Compressed. Default: Encoded] [Compression Level: 0 - Fastest, 1 - Fast, 2 - Normal, 3 - Slow, 4 - Slowest. Default: Normal]");
                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string baseNarPath = args[0];
            string secondArg = args[1];

            if (!File.Exists(baseNarPath))
                throw new FileNotFoundException("Base NAR not found.", baseNarPath);

            if (args.Length >= 3)
                storeType = (NexonArchiveFileEntryType)int.Parse(args[2]);
            if (args.Length >= 4)
                compressionLevel = (NexonArchiveFileCompressionLevel)int.Parse(args[3]);

            if (Directory.Exists(secondArg))
            {
                BuildNap(baseNarPath, secondArg);
            }
            else if (File.Exists(secondArg) && Path.GetExtension(secondArg).Equals(".nap", StringComparison.OrdinalIgnoreCase))
            {
                ApplyNap(baseNarPath, secondArg);
            }
            else
            {
                throw new ArgumentException("Second argument must be a patch folder or a NAP file.");
            }
        }

        private static void BuildNap(string baseNarPath, string patchFolder)
        {
            Console.WriteLine("Loading base NAR...");
            var baseNar = new NexonArchive();
            baseNar.Load(baseNarPath, false);

            uint prePatchCRC = CalculateArchiveCRC(baseNar);
            Console.WriteLine($"Pre-patch CRC: {prePatchCRC:X8}");
            Console.WriteLine("============================================");
            Console.WriteLine("Creating NAP...");
            Console.WriteLine("============================================");

            string parentPath = Directory.GetParent(patchFolder).FullName;

            string napPath = Path.GetFileNameWithoutExtension(baseNarPath) + ".nap";

            using (var nap = new NexonArchiveExtension(napPath))
            {
                AddFolderToArchive(nap, patchFolder, parentPath);
                nap.Save(true, true, prePatchCRC);
            }

            uint postPatchCRC;
            using (var napReader = new NexonArchive())
            using (var baseReader = new NexonArchive())
            {
                baseReader.Load(baseNarPath, false);
                napReader.Load(napPath, false);

                postPatchCRC = MergeNapIntoNar(baseReader, napReader, Path.GetTempFileName());
            }

            Console.WriteLine($"Post-patch CRC: {postPatchCRC:X8}");

            using (var napExt = new NexonArchiveExtension(napPath))
            {
                napExt.RewritePatchCRCs(prePatchCRC, postPatchCRC);
            }

            string finalNapName = Path.GetFileNameWithoutExtension(baseNarPath) + $"_{prePatchCRC:x8}_{postPatchCRC:x8}.nap";

            if (File.Exists(finalNapName))
                File.Delete(finalNapName);

            File.Move(napPath, finalNapName);

            Console.WriteLine("NAP created successfully.");

            ApplyNap(baseNarPath, finalNapName);
        }

        private static void ApplyNap(string baseNarPath, string napPath)
        {
            Console.WriteLine("============================================");
            Console.WriteLine("Applying NAP...");
            Console.WriteLine("============================================");

            var baseNar = new NexonArchive();
            baseNar.Load(baseNarPath, false);

            var nap = new NexonArchive();
            nap.Load(napPath, false);

            uint baseCRC = CalculateArchiveCRC(baseNar);
            uint prePatchCRC = nap.PrePatchCRC;
            uint postPatchCRC = nap.PostPatchCRC;

            if (baseCRC == postPatchCRC)
            {
                Console.WriteLine("NAR is already patched. Exiting.");
                return;
            }

            if (baseCRC != prePatchCRC)
            {
                throw new InvalidOperationException($"Pre-patch CRC mismatch! Expected {prePatchCRC:X8}, got {baseCRC:X8}");
            }

            string patchedNarPath = Path.GetFileNameWithoutExtension(baseNarPath) + $"_{postPatchCRC:x8}.nar";

            uint finalCRC = MergeNapIntoNar(baseNar, nap, patchedNarPath);

            if (finalCRC != postPatchCRC)
            {
                throw new InvalidOperationException($"Post-patch CRC mismatch! Expected {postPatchCRC:X8}, got {finalCRC:X8}");
            }

            Console.WriteLine("NAP applied successfully.");
        }

        private static uint MergeNapIntoNar(NexonArchive baseNar, NexonArchive nap, string outputNarPath)
        {
            using (var output = new NexonArchiveExtension(outputNarPath))
            {
                HashSet<string> napFiles = nap.FileEntries.Select(e => e.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var entry in baseNar.FileEntries)
                {
                    if (!napFiles.Contains(entry.Path))
                        CopyRawEntry(baseNar, output, entry);
                }

                foreach (var entry in nap.FileEntries)
                    CopyRawEntry(nap, output, entry);

                output.Save(true, false);
            }

            using (var verify = new NexonArchive())
            {
                verify.Load(outputNarPath, false);
                return CalculateArchiveCRC(verify);
            }
        }

        private static void AddFolderToArchive(NexonArchiveExtension archive, string folder, string parentPath)
        {
            foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            {
                string path = NormalizePath(file, parentPath);

                Console.WriteLine("--------------------------------------------");
                Console.WriteLine($"Processing file '{file}'.");
                Console.WriteLine($"Path: {path}");
                Console.WriteLine($"Type: {storeType}");
                if (storeType == NexonArchiveFileEntryType.EncodedAndCompressed)
                {
                    Console.WriteLine($"Compression Level: {compressionLevel}");
                }

                using (FileStream fs = File.OpenRead(file))
                {
                    archive.Add(fs, path, storeType, File.GetLastWriteTime(file), compressionLevel);
                }

                Console.WriteLine("--------------------------------------------");
            }
        }

        private static string NormalizePath(string file, string parent)
        {
            string p = file.Replace(parent, "").Replace('\\', '/');
            return p.StartsWith("/") ? p : "/" + p;
        }

        private static void CopyRawEntry(NexonArchive source, NexonArchiveExtension dest, NexonArchiveFileEntry entry)
        {
            Stream src = source.Stream;
            Stream dst = dest.TempFile;

            long newOffset = dst.Length;
            byte[] buffer = new byte[8192];

            lock (src)
            {
                src.Position = entry.Offset;

                long remaining = entry.CompressedSize;
                while (remaining > 0)
                {
                    int read = src.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                    if (read <= 0)
                        throw new EndOfStreamException();

                    dst.Write(buffer, 0, read);
                    remaining -= read;
                }
            }

            dest.FileEntriesInternalAdd(entry.Path, entry.StoredType, newOffset, entry.CompressedSize, entry.Size, entry.LastModifiedTime, (uint)entry.Checksum);
        }

        private static uint CalculateArchiveCRC(NexonArchive nar)
        {
            uint crc = 0;
            foreach (var entry in nar.FileEntries)
                crc ^= (uint)entry.Checksum;
            return crc;
        }
    }
}

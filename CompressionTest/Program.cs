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
using System.Security.Cryptography;

namespace Nexon.CompressionTest
{
    static class Program
    {
        static void Main(string[] args)
        {
            // NOTE: This should be used as a debugging tool only!

            if (args.Length < 1)
            {
                Console.Error.WriteLine("Test file name argument not specified.");
                return;
            }

            using (FileStream fileStream = new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[8192];
                int readLength;

                byte[] fileStreamHash;
                using (SHA512 hash = SHA512.Create())
                {
                    fileStreamHash = hash.ComputeHash(fileStream);
                    fileStream.Position = 0;
                }

                using (FileStream compressedStream = new FileStream("compressed.bin", FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
                {
                    DateTime startTime = DateTime.Now;

                    using (NexonArchiveFileCompressStream compressorStream = new NexonArchiveFileCompressStream(compressedStream, NexonArchiveFileCompressionLevel.Slowest, true))
                    {
                        while ((readLength = fileStream.Read(buffer, 0, 8192)) > 0)
                        {
                            compressorStream.Write(buffer, 0, readLength);
                        }
                    }

                    DateTime endTime = DateTime.Now;

                    Console.WriteLine("Finished compressing file. Compression time: " + (endTime - startTime));

                    using (FileStream tempFileStream = new FileStream("decompressed.bin", FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
                    {
                        startTime = DateTime.Now;

                        compressedStream.Position = 0;
                        using (NexonArchiveFileDecompressStream decompressorStream = new NexonArchiveFileDecompressStream(compressedStream, readLength, true))
                        {
                            while ((readLength = decompressorStream.Read(buffer, 0, 8192)) > 0)
                            {
                                tempFileStream.Write(buffer, 0, readLength);
                                tempFileStream.Flush();
                            }
                        }

                        endTime = DateTime.Now;

                        Console.WriteLine("Finished decompressing file. Decompression time: " + (endTime - startTime));

                        if (tempFileStream.Length != fileStream.Length)
                        {
                            Console.Error.WriteLine("ERROR: File size check failed!");
                        }
                        else
                        {
                            byte[] tempFileStreamHash;
                            using (SHA512 hash = SHA512.Create())
                            {
                                tempFileStream.Position = 0;
                                tempFileStreamHash = hash.ComputeHash(tempFileStream);
                            }

                            System.Diagnostics.Debug.Assert(fileStreamHash.Length == tempFileStreamHash.Length);
                            for (int i = 0; i < fileStreamHash.Length; ++i)
                            {
                                if (fileStreamHash[i] != tempFileStreamHash[i])
                                {
                                    Console.Error.WriteLine("ERROR: Hash check failed!");
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

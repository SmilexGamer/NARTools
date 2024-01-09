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

namespace Nexon
{
    public static class ModelHelper
    {
        private static readonly byte[] Version20Key = {
            0x32, 0xA6, 0x21, 0xE0, 0xAB, 0x6B, 0xF4, 0x2C,
            0x93, 0xC6, 0xF1, 0x96, 0xFB, 0x38, 0x75, 0x68,
            0xBA, 0x70, 0x13, 0x86, 0xE0, 0xB3, 0x71, 0xF4,
            0xE3, 0x9B, 0x07, 0x22, 0x0C, 0xFE, 0x88, 0x3A
        };
        private static readonly byte[] Version21Key = {
            0x22, 0x7A, 0x19, 0x6F, 0x7B, 0x86, 0x7D, 0xE0,
            0x8C, 0xC6, 0xF1, 0x96, 0xFB, 0x38, 0x75, 0x68,
            0x88, 0x7A, 0x78, 0x86, 0x78, 0x86, 0x67, 0x70,
            0xD9, 0x91, 0x07, 0x3A, 0x14, 0x74, 0xFE, 0x22
        };

        private static void TransformChunk(ICryptoTransform transform, byte[] data, int offset, int length)
        {
            System.Diagnostics.Debug.Assert(transform != null);
            System.Diagnostics.Debug.Assert(data != null);
            System.Diagnostics.Debug.Assert(offset >= 0);
            System.Diagnostics.Debug.Assert(length >= 0);
            System.Diagnostics.Debug.Assert((offset + length) <= data.Length);

            if (length == 0)
                return;

            // Transform the data.
            while (length > 0)
            {
                // This is a strange way to decrypt data in blocks... it would
                // be so much easier to just decrypt it in 8-byte blocks instead
                // of doing [0 or (1..128)*8] byte blocks.
                int tempLength = length;
                if (tempLength > 1024)
                    tempLength = 1024;
                if ((tempLength & 7) != 0 || tempLength == 0)
                    return;

                int decryptedLength = transform.TransformBlock(data, offset, tempLength, data, offset);
                System.Diagnostics.Debug.Assert(decryptedLength == tempLength);
                length -= decryptedLength;
                offset += decryptedLength;
            }
        }

        public static ModelResult DecryptModel(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.RandomAccess))
            {
                return DecryptModel(fs);
            }
        }

        public static ModelResult DecryptModel(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanRead)
                throw new ArgumentException("Cannot read from stream.", "stream");
            if (!stream.CanWrite)
                throw new ArgumentException("Cannot write to stream.", "stream");
            if (!stream.CanSeek)
                throw new ArgumentException("Cannot seek in stream.", "stream");

            // Seek to the beginning of the stream.
            stream.Seek(0, SeekOrigin.Begin);

            // Do not allow input files larger than the maximum integer size.
            // Also do not allow inputs smaller than the header size (244 bytes).
            if (stream.Length > int.MaxValue || stream.Length < 244)
                return ModelResult.InvalidModel;

            // Create a binary reader and binary writer.
            BinaryReader reader = new BinaryReader(stream);
            BinaryWriter writer = new BinaryWriter(stream);

            int version;
            try
            {
                // Check the file signature.
                if (reader.ReadInt32() != 0x54534449)
                    return ModelResult.InvalidModel;

                // Get the file version.
                version = reader.ReadInt32();

                // Check the file size. (Not sure if this should be checked before
                // or after the version check.)
                stream.Seek(72, SeekOrigin.Begin);
                if (reader.ReadInt32() < stream.Length)
                    return ModelResult.InvalidModel;
            }
            catch (EndOfStreamException)
            {
                return ModelResult.InvalidModel;
            }

            // Initialize algorithm and other parameters. (They may change
            // between model versions.)
            SymmetricAlgorithm algorithm = null;
            switch (version)
            {
                case 20:
                    algorithm = new Ice(4);
                    algorithm.Key = Version20Key;
                    break;
                case 21:
                    algorithm = new Ice(4);
                    algorithm.Key = Version21Key;
                    break;
                default:
                    return ModelResult.NotEncrypted;
            }
            System.Diagnostics.Debug.Assert(algorithm != null);

            using (algorithm)
            {
                // Get the decryption transform.
                using (ICryptoTransform transform = algorithm.CreateDecryptor())
                {
                    // Fix the version number (to a non-encrypted one).
                    stream.Seek(4, SeekOrigin.Begin);
                    writer.Write(9);

                    try
                    {
                        // Get textures info.
                        stream.Seek(180, SeekOrigin.Begin);
                        int numTextures = reader.ReadInt32();
                        int textureIndex = reader.ReadInt32();
                        // Enumerate textures.
                        for (int i = 0; i < numTextures && textureIndex >= 0; ++i, textureIndex += 80)
                        {
                            try
                            {
                                stream.Seek(textureIndex + 68, SeekOrigin.Begin);
                                int width = reader.ReadInt32();
                                int height = reader.ReadInt32();
                                int index = reader.ReadInt32();
                                if (width < 0 || height < 0 || index < 0)
                                    continue;

                                // Decrypt texture data.
                                stream.Seek(index, SeekOrigin.Begin);
                                byte[] textureData = reader.ReadBytes((width * height) + 768);
                                TransformChunk(transform, textureData, 0, textureData.Length);
                                stream.Seek(index, SeekOrigin.Begin);
                                writer.Write(textureData, 0, textureData.Length);
                            }
                            catch (EndOfStreamException)
                            {
                            }
                        }
                    }
                    catch (EndOfStreamException)
                    {
                    }

                    try
                    {
                        // Get body parts info.
                        stream.Seek(204, SeekOrigin.Begin);
                        int numBodyParts = reader.ReadInt32();
                        int bodyPartIndex = reader.ReadInt32();
                        // Decrypt body parts.
                        for (int i = 0; i < numBodyParts && bodyPartIndex >= 0; ++i, bodyPartIndex += 76)
                        {
                            try
                            {
                                stream.Seek(bodyPartIndex + 64, SeekOrigin.Begin);
                                int numModels = reader.ReadInt32();
                                stream.Seek(4, SeekOrigin.Current); // unused field
                                int modelIndex = reader.ReadInt32();
                                if (modelIndex < 0)
                                    continue;

                                // Enumerate models.
                                for (int j = 0; j < numModels && modelIndex >= 0; ++j, modelIndex += 112)
                                {
                                    try
                                    {
                                        stream.Seek(modelIndex + 80, SeekOrigin.Begin);
                                        int numVerts = reader.ReadInt32();
                                        stream.Seek(4, SeekOrigin.Current); // unused field
                                        int vertsIndex = reader.ReadInt32();
                                        if (vertsIndex < 0)
                                            continue;

                                        // Decrypt verticies.
                                        stream.Seek(vertsIndex, SeekOrigin.Begin);
                                        byte[] vertexData = reader.ReadBytes(numVerts * 12);
                                        TransformChunk(transform, vertexData, 0, vertexData.Length);
                                        stream.Seek(vertsIndex, SeekOrigin.Begin);
                                        writer.Write(vertexData, 0, vertexData.Length);
                                    }
                                    catch (EndOfStreamException)
                                    {
                                    }
                                }
                            }
                            catch (EndOfStreamException)
                            {
                            }
                        }
                    }
                    catch (EndOfStreamException)
                    {
                    }
                }
            }

            return ModelResult.Success;
        }
    }

    public enum ModelResult
    {
        Success = 0,
        InvalidModel,
        NotEncrypted,
    }
}

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
using System.Security.Cryptography;

namespace Nexon
{
    public sealed class Ice : SymmetricAlgorithm
    {
        #region Fields

        private int n;

        #endregion

        #region Constructors

        public Ice()
            : this(0)
        {
        }

        public Ice(int n)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException("n");

            this.n = n;
            this.ModeValue = CipherMode.ECB;
            this.PaddingValue = PaddingMode.None;
            this.BlockSizeValue = 64;
            this.LegalBlockSizesValue = new KeySizes[] {
                new KeySizes(this.BlockSizeValue, this.BlockSizeValue, 0)
            };
            this.KeySizeValue = Math.Max(n * 64, 64);
            this.LegalKeySizesValue = new KeySizes[] {
                new KeySizes(this.KeySizeValue, this.KeySizeValue, 0)
            };
        }

        #endregion

        #region Properties

        public override CipherMode Mode
        {
            get { return base.Mode; }
            set
            {
                if (value != CipherMode.ECB)
                    throw new NotSupportedException("Only ECB is currently supported.");
                base.Mode = value;
            }
        }

        public override PaddingMode Padding
        {
            get { return base.Padding; }
            set
            {
                if (value != PaddingMode.None)
                    throw new NotSupportedException("No padding is currently supported.");
                base.Padding = value;
            }
        }

        #endregion

        #region Methods

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            if (rgbKey == null)
                throw new ArgumentNullException("rgbKey");
            //if (rgbIV == null)
            //    throw new ArgumentNullException("rgbIV");
            if (rgbKey.Length != (this.KeySizeValue / 8))
                throw new ArgumentException("Key size is not valid.", "rgbKey");
            //if (rgbIV.Length != (this.BlockSizeValue / 8))
            //    throw new ArgumentException("IV size is not valid.", "rgbIV");

            return new IceCryptoTransform(this.n, rgbKey, false);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            if (rgbKey == null)
                throw new ArgumentNullException("rgbKey");
            //if (rgbIV == null)
            //    throw new ArgumentNullException("rgbIV");
            if (rgbKey.Length != (this.KeySizeValue / 8))
                throw new ArgumentException("Key size is not valid.", "rgbKey");
            //if (rgbIV.Length != (this.BlockSizeValue / 8))
            //    throw new ArgumentException("IV size is not valid.", "rgbIV");

            return new IceCryptoTransform(this.n, rgbKey, true);
        }

        public override void GenerateIV()
        {
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] iv = new byte[8];
            rng.GetBytes(iv);
            this.IVValue = iv;
        }

        public override void GenerateKey()
        {
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] key = new byte[this.KeySizeValue / 8];
            rng.GetBytes(key);
            this.KeyValue = key;
        }

        #endregion
    }

    internal sealed class IceCryptoTransform : ICryptoTransform
    {
        #region Fields

        private static uint[,] SBox;
        private static readonly int[,] SMod = {
            { 333, 313, 505, 369 },
            { 379, 375, 319, 391 },
            { 361, 445, 451, 397 },
            { 397, 425, 395, 505 }
        };
        private static readonly int[,] SXor = {
            { 0x83, 0x85, 0x9b, 0xcd },
            { 0xcc, 0xa7, 0xad, 0x41 },
            { 0x4b, 0x2e, 0xd4, 0x33 },
            { 0xea, 0xcb, 0x2e, 0x04 }
        };
        private static readonly uint[] PBox = {
            0x00000001, 0x00000080, 0x00000400, 0x00002000,
            0x00080000, 0x00200000, 0x01000000, 0x40000000,
            0x00000008, 0x00000020, 0x00000100, 0x00004000,
            0x00010000, 0x00800000, 0x04000000, 0x20000000,
            0x00000004, 0x00000010, 0x00000200, 0x00008000,
            0x00020000, 0x00400000, 0x08000000, 0x10000000,
            0x00000002, 0x00000040, 0x00000800, 0x00001000,
            0x00040000, 0x00100000, 0x02000000, 0x80000000
        };
        private static readonly int[] KeyRotation = {
            0, 1, 2, 3, 2, 1, 3, 0, 1, 3, 2, 0, 3, 1, 0, 2
        };

        private bool encrypt;
        private int size;
        private int rounds;
        private uint[,] keySchedule;

        #endregion

        #region Constructors

        internal IceCryptoTransform(int n, byte[] key, bool encrypt)
        {
            System.Diagnostics.Debug.Assert(n >= 0);
            System.Diagnostics.Debug.Assert(key != null);
            System.Diagnostics.Debug.Assert((key.Length & 7) == 0);

            this.encrypt = encrypt;

            InitializeSBox();

            if (n == 0)
            {
                this.size = 1;
                this.rounds = 8;
            }
            else
            {
                this.size = n;
                this.rounds = n << 4;
            }

            this.keySchedule = new uint[this.rounds, 3];

            SetKey(key);
        }

        #endregion

        #region Methods

        private static uint GFMultiply(uint a, uint b, uint m)
        {
            uint res = 0;
            while (b != 0)
            {
                if ((b & 1) != 0)
                    res ^= a;
                a <<= 1;
                b >>= 1;
                if (a >= 256)
                    a ^= m;
            }
            return res;
        }

        private static uint GFExp7(uint b, uint m)
        {
            if (b == 0)
                return 0;

            uint x;
            x = GFMultiply(b, b, m);
            x = GFMultiply(b, x, m);
            x = GFMultiply(x, x, m);
            x = GFMultiply(b, x, m);
            return x;
        }

        private static uint Perm32(uint x)
        {
            uint res = 0;
            int pbox = 0;
            while (x != 0)
            {
                if ((x & 1) != 0)
                    res |= PBox[pbox];
                ++pbox;
                x >>= 1;
            }
            return res;
        }

        private static void InitializeSBox()
        {
            if (SBox == null)
            {
                SBox = new uint[4, 1024];
                for (int i = 0; i < 1024; ++i)
                {
                    int col = (i >> 1) & 0xff;
                    int row = (i & 0x1) | ((i & 0x200) >> 8);

                    SBox[0, i] = Perm32(GFExp7((uint)(col ^ SXor[0, row]), (uint)SMod[0, row]) << 24);
                    SBox[1, i] = Perm32(GFExp7((uint)(col ^ SXor[1, row]), (uint)SMod[1, row]) << 16);
                    SBox[2, i] = Perm32(GFExp7((uint)(col ^ SXor[2, row]), (uint)SMod[2, row]) << 8);
                    SBox[3, i] = Perm32(GFExp7((uint)(col ^ SXor[3, row]), (uint)SMod[3, row]));
                }
            }
        }

        private void BuildSchedule(ushort[] keyBuilder, int n, int keyRotationOffset)
        {
            System.Diagnostics.Debug.Assert(keyBuilder != null);
            System.Diagnostics.Debug.Assert(keyBuilder.Length == 4);
            System.Diagnostics.Debug.Assert(n >= 0);
            System.Diagnostics.Debug.Assert(keyRotationOffset >= 0);

            int i, j, k;
            for (i = 0; i < 8; ++i)
            {
                int keyRotation = KeyRotation[keyRotationOffset + i];
                int subKeyIndex = n + i;

                this.keySchedule[subKeyIndex, 0] = 0;
                this.keySchedule[subKeyIndex, 1] = 0;
                this.keySchedule[subKeyIndex, 2] = 0;

                for (j = 0; j < 15; ++j)
                {
                    for (k = 0; k < 4; ++k)
                    {
                        ushort currentKeyBuilder = keyBuilder[(keyRotation + k) & 3];
                        ushort bit = (ushort)(currentKeyBuilder & 1);

                        this.keySchedule[subKeyIndex, j % 3] = (this.keySchedule[subKeyIndex, j % 3] << 1) | bit;
                        keyBuilder[(keyRotation + k) & 3] = (ushort)((currentKeyBuilder >> 1) | ((bit ^ 1) << 15));
                    }
                }
            }
        }

        private void SetKey(byte[] key)
        {
            System.Diagnostics.Debug.Assert(key != null);

            int i, j;
            ushort[] keyBuilder = new ushort[4];
            if (this.rounds == 8)
            {
                System.Diagnostics.Debug.Assert(this.size == 1);
                if (key.Length != 8)
                    throw new ArgumentException("Key size is not valid.", "key");

                for (i = 0; i < 4; ++i)
                    keyBuilder[3 - i] = (ushort)((key[i << 1] << 8) | key[(i << 1) + 1]);

                BuildSchedule(keyBuilder, 0, 0);
            }
            else
            {
                System.Diagnostics.Debug.Assert((this.size << 4) == this.rounds);
                if (key.Length != (this.size << 3))
                    throw new ArgumentException("Key size is not valid.", "key");

                for (i = 0; i < this.size; ++i)
                {
                    int pos = i << 3;

                    for (j = 0; j < 4; ++j)
                        keyBuilder[3 - j] = (ushort)((key[pos + (j << 1)] << 8) | key[pos + (j << 1) + 1]);

                    BuildSchedule(keyBuilder, pos, 0);
                    BuildSchedule(keyBuilder, this.rounds - 8 - pos, 8);
                }
            }
        }

        private uint Transform(uint value, int subKeyIndex)
        {
            System.Diagnostics.Debug.Assert(subKeyIndex >= 0);

            uint tl, tr;
            uint al, ar;

            tl = ((value >> 16) & 0x3ff) | (((value >> 14) | (value << 18)) & 0xffc00);
            tr = (value & 0x3ff) | ((value << 2) & 0xffc00);

            al = this.keySchedule[subKeyIndex, 2] & (tl ^ tr);
            ar = al ^ tr;
            al ^= tl;

            al ^= this.keySchedule[subKeyIndex, 0];
            ar ^= this.keySchedule[subKeyIndex, 1];

            return SBox[0, al >> 10] | SBox[1, al & 0x3ff] | SBox[2, ar >> 10] | SBox[3, ar & 0x3ff];
        }

        private void Encrypt(byte[] input, int inputOffset, byte[] output, int outputOffset)
        {
            System.Diagnostics.Debug.Assert(input != null);
            System.Diagnostics.Debug.Assert(inputOffset >= 0);
            System.Diagnostics.Debug.Assert((input.Length - inputOffset) >= 8);
            System.Diagnostics.Debug.Assert(output != null);
            System.Diagnostics.Debug.Assert(outputOffset >= 0);
            System.Diagnostics.Debug.Assert((output.Length - outputOffset) >= 8);

            int i;
            uint l, r;

            l = (uint)((input[inputOffset] << 24) | (input[inputOffset + 1] << 16) | (input[inputOffset + 2] << 8) | input[inputOffset + 3]);
            r = (uint)((input[inputOffset + 4] << 24) | (input[inputOffset + 5] << 16) | (input[inputOffset + 6] << 8) | input[inputOffset + 7]);

            for (i = 0; i < this.rounds; i += 2)
            {
                l ^= Transform(r, i);
                r ^= Transform(l, i + 1);
            }

            output[outputOffset] = (byte)((r >> 24) & 0xFF);
            output[outputOffset + 1] = (byte)((r >> 16) & 0xFF);
            output[outputOffset + 2] = (byte)((r >> 8) & 0xFF);
            output[outputOffset + 3] = (byte)(r & 0xFF);
            output[outputOffset + 4] = (byte)((l >> 24) & 0xFF);
            output[outputOffset + 5] = (byte)((l >> 16) & 0xFF);
            output[outputOffset + 6] = (byte)((l >> 8) & 0xFF);
            output[outputOffset + 7] = (byte)(l & 0xFF);
        }

        private void Decrypt(byte[] input, int inputOffset, byte[] output, int outputOffset)
        {
            System.Diagnostics.Debug.Assert(input != null);
            System.Diagnostics.Debug.Assert(inputOffset >= 0);
            System.Diagnostics.Debug.Assert((input.Length - inputOffset) >= 8);
            System.Diagnostics.Debug.Assert(output != null);
            System.Diagnostics.Debug.Assert(outputOffset >= 0);
            System.Diagnostics.Debug.Assert((output.Length - outputOffset) >= 8);

            int i;
            uint l, r;

            l = (uint)((input[inputOffset] << 24) | (input[inputOffset + 1] << 16) | (input[inputOffset + 2] << 8) | input[inputOffset + 3]);
            r = (uint)((input[inputOffset + 4] << 24) | (input[inputOffset + 5] << 16) | (input[inputOffset + 6] << 8) | input[inputOffset + 7]);

            for (i = this.rounds - 1; i > 0; i -= 2)
            {
                l ^= Transform(r, i);
                r ^= Transform(l, i - 1);
            }

            output[outputOffset] = (byte)((r >> 24) & 0xFF);
            output[outputOffset + 1] = (byte)((r >> 16) & 0xFF);
            output[outputOffset + 2] = (byte)((r >> 8) & 0xFF);
            output[outputOffset + 3] = (byte)(r & 0xFF);
            output[outputOffset + 4] = (byte)((l >> 24) & 0xFF);
            output[outputOffset + 5] = (byte)((l >> 16) & 0xFF);
            output[outputOffset + 6] = (byte)((l >> 8) & 0xFF);
            output[outputOffset + 7] = (byte)(l & 0xFF);
        }

        #endregion

        #region ICryptoTransform Members

        public bool CanReuseTransform
        {
            get { return false; }
        }

        public bool CanTransformMultipleBlocks
        {
            get { return true; }
        }

        public int InputBlockSize
        {
            get { return 8; }
        }

        public int OutputBlockSize
        {
            get { return 8; }
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (inputBuffer == null)
                throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException("inputOffset");
            if (inputCount < 0 || (inputOffset + inputCount) > inputBuffer.Length)
                throw new ArgumentOutOfRangeException("inputCount");
            if (outputBuffer == null)
                throw new ArgumentNullException("outputBuffer");
            if (outputOffset < 0)
                throw new ArgumentOutOfRangeException("outputOffset");
            if ((outputOffset + inputCount) > outputBuffer.Length)
                throw new ArgumentOutOfRangeException("inputCount");

            int i;
            if (this.encrypt)
            {
                for (i = 0; i < inputCount; i += 8)
                {
                    Encrypt(inputBuffer, inputOffset, outputBuffer, outputOffset);
                    inputOffset += 8;
                    outputOffset += 8;
                }
            }
            else
            {
                for (i = 0; i < inputCount; i += 8)
                {
                    Decrypt(inputBuffer, inputOffset, outputBuffer, outputOffset);
                    inputOffset += 8;
                    outputOffset += 8;
                }
            }

            return inputCount;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (inputBuffer == null)
                throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException("inputOffset");
            if (inputCount < 0 || (inputOffset + inputCount) > inputBuffer.Length)
                throw new ArgumentOutOfRangeException("inputCount");

            byte[] outputBuffer = new byte[(inputCount + 7) & (~7)];
            int outputOffset = 0;
            int i;
            if (this.encrypt)
            {
                for (i = 0; i < inputCount; i += 8)
                {
                    Encrypt(inputBuffer, inputOffset, outputBuffer, outputOffset);
                    inputOffset += 8;
                    outputOffset += 8;
                }
            }
            else
            {
                for (i = 0; i < inputCount; i += 8)
                {
                    Decrypt(inputBuffer, inputOffset, outputBuffer, outputOffset);
                    inputOffset += 8;
                    outputOffset += 8;
                }
            }

            return outputBuffer;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            this.size = 0;
            this.rounds = 0;
            // Clear the key schedule for extra security.
            for (int i = 0; i < this.keySchedule.GetLength(0); ++i)
                for (int j = 0; j < this.keySchedule.GetLength(1); ++j)
                    this.keySchedule[i, j] = 0;
            this.keySchedule = null;
        }

        #endregion
    }

}

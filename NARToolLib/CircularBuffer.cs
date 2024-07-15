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

namespace Nexon
{
    internal sealed class CircularBuffer
    {
        #region Fields

        private byte[] data;
        private int source;
        private int length;

        #endregion

        #region Constructors

        public CircularBuffer(int length)
        {
            System.Diagnostics.Debug.Assert(length > 0);
            this.data = new byte[length];
        }

        #endregion

        #region Properties

        public int Length
        {
            get { return this.length; }
        }

        //public int RealLength
        //{
        //    get { return this.data.Length; }
        //}

        #endregion

        #region Methods

        public void Append(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || (offset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("count");

            //System.Diagnostics.Debug.Assert(count <= this.data.Length);

            while (count > 0)
            {
                this.Append(buffer[offset++]);
                --count;
            }

            //// Shortcut if there is no data to append.
            //if (count == 0)
            //    return;

            //if (count >= this.data.Length)
            //{
            //    Buffer.BlockCopy(buffer, offset + (count - this.data.Length), this.data, 0, this.data.Length);
            //    this.source = 0;
            //    this.length = this.data.Length;
            //}
            //else
            //{
            //    if (this.source == this.data.Length)
            //        this.source = 0;
            //    int initialCopyLength = Math.Min(this.data.Length - this.source, count);
            //    Buffer.BlockCopy(buffer, offset, this.data, this.source, initialCopyLength);
            //    if (count > initialCopyLength)
            //        Buffer.BlockCopy(buffer, offset + initialCopyLength, this.data, 0, count - initialCopyLength);
            //    this.source = (this.source + count) % this.data.Length;
            //    this.length = Math.Min(this.length + count, this.data.Length);
            //}
        }

        public void Append(byte value)
        {
            this.data[this.source++] = value;
            if (this.source == this.data.Length)
                this.source = 0;
            if (this.length < this.data.Length)
                ++this.length;
        }

        public byte GetByteAndUpdate(int distance)
        {
            if (distance <= 0 || distance > this.length)
                throw new ArgumentOutOfRangeException("distance");

            distance = this.source - distance;
            if (distance < 0)
                distance += this.length;

            byte value = this.data[distance];
            this.Append(value);
            return value;
        }

        public void CopyAndUpdate(int distance, byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || (offset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("count");
            if (distance <= 0 || distance > this.length)
                throw new ArgumentOutOfRangeException("distance");

            while (count > 0)
            {
                buffer[offset++] = this.GetByteAndUpdate(distance);
                --count;
            }
        }

        public int Match(int distance, byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || (offset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("count");
            if (distance <= 0 || distance > this.length)
                throw new ArgumentOutOfRangeException("distance");

            return this.MatchInternal(distance, buffer, offset, count);
        }

        public int FindLongestMatch(byte[] buffer, int offset, int count, out int distance)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || (offset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("count");

            int longestMatchDistance = 0;
            int longestMatchLength = 0;

            for (int i = 1; i <= this.length; ++i)
            {
                int length = this.MatchInternal(i, buffer, offset, count);
                if (length > longestMatchLength)
                {
                    longestMatchDistance = i;
                    longestMatchLength = length;
                }
            }

            distance = longestMatchDistance;
            return longestMatchLength;
        }

        private int MatchInternal(int distance, byte[] buffer, int offset, int count)
        {
            // Find the dictionary source with the given distance.
            int source = this.source - distance;
            if (source < 0)
                source += this.length;

            int tempCount = this.length - distance;
            int matches = 0;
            while (count > 0)
            {
                // If the data does not match, then break out of this loop and
                // return however many matches there were.
                if (this.data[source] != buffer[offset++])
                    break;

                // A match was found.
                --count;
                ++matches;

                if (++tempCount == this.length)
                {
                    source = this.source - distance;
                    if (source < 0)
                        source += this.length;
                    tempCount = this.length - distance;
                }
                else if (++source == this.data.Length)
                {
                    source = 0;
                }
            }

            return matches;
        }

        #endregion
    }
}

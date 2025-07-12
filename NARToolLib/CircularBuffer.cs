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

namespace Nexon
{
    internal sealed class CircularBuffer
    {
        #region Fields

        private byte[] data;
        private int source;
        private int length;
        private int hashTableSize;

        // Hash table for fast match finding - significantly improves performance
        private Dictionary<int, LinkedList<int>> hashTable;

        #endregion

        #region Constructors

        public CircularBuffer(int length, int hashTableSize)
        {
            System.Diagnostics.Debug.Assert(length > 0);
            this.data = new byte[length];
            this.hashTableSize = hashTableSize;
            this.hashTable = new Dictionary<int, LinkedList<int>>(hashTableSize);
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

            // Shortcut if there is no data to append.
            if (count == 0)
                return;

            // Optimized version of the original bulk copy logic
            if (count >= this.data.Length)
            {
                // Clear hash table since we're overwriting everything
                this.hashTable.Clear();

                Buffer.BlockCopy(buffer, offset + (count - this.data.Length), this.data, 0, this.data.Length);
                this.source = 0;
                this.length = this.data.Length;

                // Rebuild hash table for new data
                this.RebuildHashTable();
            }
            else
            {
                // Use original logic but update hash table incrementally
                if (this.source == this.data.Length)
                    this.source = 0;
                int initialCopyLength = Math.Min(this.data.Length - this.source, count);

                // Update hash table for bytes being overwritten
                for (int i = 0; i < initialCopyLength; i++)
                {
                    int pos = this.source + i;
                    if (this.length == this.data.Length)
                    {
                        this.RemoveFromHashTable(pos);
                    }
                }

                Buffer.BlockCopy(buffer, offset, this.data, this.source, initialCopyLength);

                // Add new bytes to hash table
                for (int i = 0; i < initialCopyLength; i++)
                {
                    this.AddToHashTable(this.source + i, this.data[this.source + i]);
                }

                if (count > initialCopyLength)
                {
                    // Update hash table for second part
                    for (int i = 0; i < count - initialCopyLength; i++)
                    {
                        if (this.length == this.data.Length)
                        {
                            this.RemoveFromHashTable(i);
                        }
                    }

                    Buffer.BlockCopy(buffer, offset + initialCopyLength, this.data, 0, count - initialCopyLength);

                    // Add new bytes to hash table
                    for (int i = 0; i < count - initialCopyLength; i++)
                    {
                        this.AddToHashTable(i, this.data[i]);
                    }
                }

                this.source = (this.source + count) % this.data.Length;
                this.length = Math.Min(this.length + count, this.data.Length);
            }
        }

        public void Append(byte value)
        {
            // Remove old hash entry if we're overwriting
            if (this.length == this.data.Length)
            {
                this.RemoveFromHashTable(this.source);
            }

            this.data[this.source++] = value;
            if (this.source == this.data.Length)
                this.source = 0;
            if (this.length < this.data.Length)
                ++this.length;

            // Add to hash table (use the previous source position)
            int hashPos = this.source - 1;
            if (hashPos < 0)
                hashPos = this.data.Length - 1;
            this.AddToHashTable(hashPos, value);
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

            // Use original logic to maintain correctness
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

            distance = 0;

            // Early exit optimizations
            if (count == 0 || this.length == 0)
                return 0;

            // Use hash table for fast candidate finding
            byte firstByte = buffer[offset];
            int hash = this.HashFunction(firstByte);

            LinkedList<int> candidates;
            if (!this.hashTable.TryGetValue(hash, out candidates) || candidates.Count == 0)
                return 0;

            int longestMatchDistance = 0;
            int longestMatchLength = 0;

            // Check candidates from hash table (most recent first for better cache locality)
            for (var node = candidates.Last; node != null; node = node.Previous)
            {
                int pos = node.Value;
                int candidateDistance = this.source - pos;
                if (candidateDistance <= 0)
                    candidateDistance += this.length;

                // Quick check: if this position is too far to beat our current best, skip
                if (candidateDistance > this.length)
                    continue;

                int matchLength = this.MatchInternal(candidateDistance, buffer, offset, count);
                if (matchLength > longestMatchLength)
                {
                    longestMatchDistance = candidateDistance;
                    longestMatchLength = matchLength;

                    // Early termination if we found a very good match
                    if (matchLength == count)
                        break;
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

        private void AddToHashTable(int position, byte value)
        {
            int hash = this.HashFunction(value);
            LinkedList<int> positions;

            if (!this.hashTable.TryGetValue(hash, out positions))
            {
                positions = new LinkedList<int>();
                this.hashTable[hash] = positions;
            }

            positions.AddLast(position);

            // Limit hash chain length to prevent performance degradation
            if (positions.Count > this.hashTableSize)
            {
                positions.RemoveFirst();
            }
        }

        private void RemoveFromHashTable(int position)
        {
            if (position >= this.length)
                return;

            byte value = this.data[position];
            int hash = this.HashFunction(value);
            LinkedList<int> positions;

            if (this.hashTable.TryGetValue(hash, out positions))
            {
                positions.Remove(position);
                if (positions.Count == 0)
                {
                    this.hashTable.Remove(hash);
                }
            }
        }

        private int HashFunction(byte value)
        {
            // Simple hash function for single byte
            return value % this.hashTableSize;
        }

        private void RebuildHashTable()
        {
            this.hashTable.Clear();

            for (int i = 0; i < this.length; i++)
            {
                this.AddToHashTable(i, this.data[i]);
            }
        }

        #endregion
    }
}
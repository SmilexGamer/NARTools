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
        private int hashTableSize;

        // Array-based hash chains: no per-node allocations.
        // For each hash bucket we keep head (oldest) and tail (newest).
        private int[] head;
        private int[] tail;
        private int[] next; // next newer
        private int[] prev; // previous older
        private int[] bucketCount;

        #endregion

        #region Constructors

        public CircularBuffer(int length, int hashTableSize)
        {
            System.Diagnostics.Debug.Assert(length > 0);
            System.Diagnostics.Debug.Assert(hashTableSize > 0);

            this.data = new byte[length];
            this.hashTableSize = hashTableSize;

            this.head = new int[hashTableSize];
            this.tail = new int[hashTableSize];
            this.next = new int[length];
            this.prev = new int[length];
            this.bucketCount = new int[hashTableSize];

            for (int i = 0; i < hashTableSize; i++)
            {
                this.head[i] = -1;
                this.tail[i] = -1;
                this.bucketCount[i] = 0;
            }
            for (int i = 0; i < length; i++)
            {
                this.next[i] = -1;
                this.prev[i] = -1;
            }
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

            // If we are going to overwrite the whole ring, copy last N bytes and rebuild.
            if (count >= this.data.Length)
            {
                // Clear bucket metadata and copy last N bytes
                for (int i = 0; i < this.hashTableSize; i++)
                {
                    this.head[i] = -1;
                    this.tail[i] = -1;
                    this.bucketCount[i] = 0;
                }
                for (int i = 0; i < this.data.Length; i++)
                {
                    this.next[i] = -1;
                    this.prev[i] = -1;
                }

                Buffer.BlockCopy(buffer, offset + (count - this.data.Length), this.data, 0, this.data.Length);
                this.source = 0;
                this.length = this.data.Length;

                // Rebuild hash chains for new data
                this.RebuildHashTable();
                return;
            }

            // Normal incremental append: keep original semantics by appending one-by-one.
            for (int i = 0; i < count; i++)
            {
                Append(buffer[offset + i]);
            }
        }

        public void Append(byte value)
        {
            // If buffer is full we will overwrite position 'source' -> remove that position from its bucket
            if (this.length == this.data.Length)
            {
                // Remove the existing entry for 'source' (oldest physical slot that will be overwritten)
                this.RemoveFromHashTable(this.source);
            }

            // Write the byte at the current source.
            this.data[this.source] = value;

            // Insert this position into its bucket as newest (append to tail), preserving original ordering.
            int hash = this.HashFunction(value);
            InsertPositionToBucketTail(hash, this.source);

            // Advance source and update logical length.
            if (++this.source == this.data.Length)
                this.source = 0;
            if (this.length < this.data.Length)
                ++this.length;
        }

        public byte GetByteAndUpdate(int distance)
        {
            if (distance <= 0 || distance > this.length)
                throw new ArgumentOutOfRangeException("distance");

            int idx = this.source - distance;
            if (idx < 0)
                idx += this.data.Length;

            byte value = this.data[idx];
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
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (offset < 0) throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || (offset + count) > buffer.Length) throw new ArgumentOutOfRangeException("count");
            if (distance <= 0 || distance > this.length) throw new ArgumentOutOfRangeException("distance");

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

            int longestMatchDistance = 0;
            int longestMatchLength = 0;

            // Traverse candidates from newest to oldest (original iterated candidates.Last to First).
            int pos = this.tail[hash]; // newest
            while (pos != -1)
            {
                int candidateDistance = this.source - pos;
                if (candidateDistance <= 0)
                    candidateDistance += this.length;

                // Quick check: if this position is too far to be valid, skip
                if (candidateDistance <= this.length && candidateDistance > 0)
                {
                    int matchLength = this.MatchInternal(candidateDistance, buffer, offset, count);
                    if (matchLength > longestMatchLength)
                    {
                        longestMatchDistance = candidateDistance;
                        longestMatchLength = matchLength;

                        // Early termination if we found a perfect match
                        if (matchLength == count)
                            break;
                    }
                }

                pos = this.prev[pos]; // move to older
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
            // This method mirrors original behavior but uses array chains.
            int h = this.HashFunction(value);
            InsertPositionToBucketTail(h, position);

            // If bucket grows beyond hashTableSize, remove the oldest (head) to match original behavior.
            if (++this.bucketCount[h] > this.hashTableSize)
            {
                int oldest = this.head[h];
                if (oldest != -1)
                {
                    RemovePositionFromBucket(h, oldest);
                    --this.bucketCount[h];
                }
            }
        }

        private void RemoveFromHashTable(int position)
        {
            // Mirror original early-exit check: ignore positions outside the logical 'length'
            if (position >= this.length)
                return;

            byte value = this.data[position];
            int h = this.HashFunction(value);

            // Remove position from its bucket if present
            int cur = this.head[h];
            while (cur != -1)
            {
                if (cur == position)
                {
                    RemovePositionFromBucket(h, position);
                    --this.bucketCount[h];
                    break;
                }
                cur = this.next[cur];
            }
        }

        private int HashFunction(byte value)
        {
            // Simple hash function for single byte
            return value % this.hashTableSize;
        }

        private void RebuildHashTable()
        {
            // Clear buckets
            for (int i = 0; i < this.hashTableSize; i++)
            {
                this.head[i] = -1;
                this.tail[i] = -1;
                this.bucketCount[i] = 0;
            }
            for (int i = 0; i < this.next.Length; i++)
            {
                this.next[i] = -1;
                this.prev[i] = -1;
            }

            // Insert in chronological order (oldest -> newest) so tail becomes newest
            for (int i = 0; i < this.length; i++)
            {
                int pos = this.source - this.length + i;
                if (pos < 0) pos += this.data.Length;
                AddToHashTable(pos, this.data[pos]);
            }
        }

        #endregion

        #region Bucket helper methods

        // Insert position as newest (append to tail) in bucket h.
        private void InsertPositionToBucketTail(int h, int position)
        {
            // unlink indices should be clean
            this.next[position] = -1;
            this.prev[position] = -1;

            if (this.tail[h] == -1)
            {
                // empty bucket
                this.head[h] = position;
                this.tail[h] = position;
            }
            else
            {
                // append to tail
                int oldTail = this.tail[h];
                this.next[oldTail] = position;
                this.prev[position] = oldTail;
                this.tail[h] = position;
            }
            this.bucketCount[h]++;
        }

        // Remove a specific position from bucket h (internal, assumes position exists)
        private void RemovePositionFromBucket(int h, int position)
        {
            int p = this.prev[position];
            int n = this.next[position];

            if (p != -1)
                this.next[p] = n;
            else
                this.head[h] = n; // position was head (oldest)

            if (n != -1)
                this.prev[n] = p;
            else
                this.tail[h] = p; // position was tail (newest)

            this.prev[position] = -1;
            this.next[position] = -1;
        }

        #endregion
    }
}

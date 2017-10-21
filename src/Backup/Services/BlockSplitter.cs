using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Backup.Services
{
    public class BlockSplitter
    {
        private const int width = 64;  //--> the # of bytes in the window
        private const long seed = 2273;  //--> a our hash seed
        private const long mask = (1 << 16) - 1;  //--> a hash seive: 16 gets you ~64k chunks
        private const int circleSize = Constants.MaxBlockSize; //--> circle buffer size. Will also be the max block size

        private long maxSeed;
        private byte[] circle = new byte[circleSize];
        //private long streamPos;
        private long hash;  //--> our rolling hash
        private int hashEnd;
        private int hashStart;
        private int start;

        public bool HasRemainingBytes => start != hashEnd;

        public void Initialize()
        {
            hash = 0L;  //--> our rolling hash
            hashEnd = 0;
            hashStart = 0;
            start = 0;

            maxSeed = seed; //--> will be prime^width after initialization (sorta)

            //--> initialize maxSeed...
            for (int i = 0; i < width; i++) maxSeed *= maxSeed;

        }

        public IList<byte[]> NextBlock(byte[] buffer, int offset, int count = -1)
        {
            var currentReadPos = offset;
            var maxReadPos = currentReadPos + (count > -1 ? count : buffer.Length - offset);
            var blocksList = new List<byte[]>();

            maxReadPos = Math.Min(maxReadPos, buffer.Length);

            while (currentReadPos < maxReadPos)
            {
                if ((hashEnd - hashStart) <= width)
                    hash = buffer[currentReadPos] + (hash * seed);
                else
                    hash = buffer[currentReadPos] + ((hash - (maxSeed * circle[hashStart++])) * seed);

                circle[hashEnd++] = buffer[currentReadPos];

                if (hashEnd == circleSize) hashEnd = 0;
                if (hashStart == circleSize) hashStart = 0;

                if (((hash | mask) == hash)
                    || (hashEnd == start))
                {
                    var _start = start;
                    start = hashStart = hashEnd;
                    hash = 0;

                    blocksList.Add(BufferToBlock(circle, _start, hashEnd));

                }
                currentReadPos++;
            }

            return blocksList;
        }

        public byte[] RemainingBytes()
        {
            if (start == hashEnd)
                return new byte[0];

            return BufferToBlock(circle, start, hashEnd);
        }

        private static byte[] BufferToBlock(byte[] buffer, int start, int end)
        {
            int size;
            byte[] blockBuffer;

            if (end > start)
            {
                size = end - start;
                blockBuffer = new byte[size];
                Array.Copy(buffer, start, blockBuffer, 0, size);
            }
            else
            {
                size = buffer.Length - start + end;
                blockBuffer = new byte[size];
                Array.Copy(buffer, start, blockBuffer, 0, buffer.Length - start);
                Array.Copy(buffer, 0, blockBuffer, buffer.Length - start, end);
            }

            return blockBuffer;
        }

    }
}

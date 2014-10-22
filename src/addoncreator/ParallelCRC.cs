using System;
using System.Threading;

namespace CRC32
{
    public class ParallelCRC
    {
        private const uint kCrcPoly = 0xEDB88320;
        private const uint kInitial = 0xFFFFFFFF;
        private const int CRC_NUM_TABLES = 8;

        private const int ThreadCost = 256 << 10;
        private static readonly uint[] Table;
        private static readonly int ThreadCount = Environment.ProcessorCount;


        private uint value;

        static ParallelCRC()
        {
            unchecked
            {
                Table = new uint[256*CRC_NUM_TABLES];
                int i;
                for (i = 0; i < 256; i++)
                {
                    var r = (uint) i;
                    for (int j = 0; j < 8; j++)
                        r = (r >> 1) ^ (kCrcPoly & ~((r & 1) - 1));
                    Table[i] = r;
                }
                for (; i < 256*CRC_NUM_TABLES; i++)
                {
                    uint r = Table[i - 256];
                    Table[i] = Table[r & 0xFF] ^ (r >> 8);
                }
            }
        }

        public ParallelCRC()
        {
            Init();
        }

        public int Value
        {
            get { return (int) ~value; }
        }

        /// <summary>
        ///     Reset CRC
        /// </summary>
        public void Init()
        {
            value = kInitial;
        }

        public void UpdateByte(byte b)
        {
            value = (value >> 8) ^ Table[(byte) value ^ b];
        }

        public void Update(byte[] data, int offset, int count)
        {
            new ArraySegment<byte>(data, offset, count); // check arguments

            if (count <= ThreadCost || ThreadCount <= 1)
            {
                value = ProcessBlock(value, data, offset, count);
                return;
            }

            // choose optimal number of threads to use

            int threadCount = ThreadCount;
            L0:
            int bytesPerThread = (count + threadCount - 1)/threadCount;
            if (bytesPerThread < ThreadCost >> 1)
            {
                threadCount--;
                goto L0;
            }

            // threadCount >= 2

            Job lastJob = null;
            while (count > bytesPerThread)
            {
                var job = new Job(new ArraySegment<byte>(data, offset, bytesPerThread), this, lastJob);
                ThreadPool.QueueUserWorkItem(job.Do);
                offset += bytesPerThread;
                count -= bytesPerThread;
                lastJob = job;
            }

            // lastJob != null
            var lastBlockCRC = ProcessBlock(kInitial, data, offset, count);
            lastJob.WaitAndDispose();
            value = Combine(value, lastBlockCRC, count);
        }

        private static uint ProcessBlock(uint crc, byte[] data, int offset, int count)
        {
            /*
             * A copy of Optimized implementation.
             */

            if (count < 0) throw new ArgumentOutOfRangeException("count");
            if (count == 0) return crc;

            var table = Table;

            for (; (offset & 7) != 0 && count != 0; count--)
                crc = (crc >> 8) ^ table[(byte) crc ^ data[offset++]];

            if (count >= 8)
            {
                int to = (count - 8) & ~7;
                count -= to;
                to += offset;

                while (offset != to)
                {
                    crc ^=
                        (uint)
                            (data[offset] + (data[offset + 1] << 8) + (data[offset + 2] << 16) +
                             (data[offset + 3] << 24));
                    var high =
                        (uint)
                            (data[offset + 4] + (data[offset + 5] << 8) + (data[offset + 6] << 16) +
                             (data[offset + 7] << 24));
                    offset += 8;

                    crc = table[(byte) crc + 0x700]
                          ^ table[(byte) (crc >>= 8) + 0x600]
                          ^ table[(byte) (crc >>= 8) + 0x500]
                          ^ table[ /*(byte)*/(crc >> 8) + 0x400]
                          ^ table[(byte) (high) + 0x300]
                          ^ table[(byte) (high >>= 8) + 0x200]
                          ^ table[(byte) (high >>= 8) + 0x100]
                          ^ table[ /*(byte)*/(high >> 8) + 0x000];
                }
            }

            while (count-- != 0)
                crc = (crc >> 8) ^ table[(byte) crc ^ data[offset++]];

            return crc;
        }

        public static int Compute(byte[] data, int offset, int count)
        {
            var crc = new ParallelCRC();
            crc.Update(data, offset, count);
            return crc.Value;
        }

        public static int Compute(byte[] data)
        {
            return Compute(data, 0, data.Length);
        }

        public static int Compute(ArraySegment<byte> block)
        {
            return Compute(block.Array, block.Offset, block.Count);
        }

        #region Combining

        /*
         * CRC values combining algorithm.
         * Taken from DotNetZip project sources (http://dotnetzip.codeplex.com/)
         */

        private static uint[] even_cache;
        private static uint[] odd_cache;

        /// <summary>
        ///     This function is thread-safe!
        /// </summary>
        private static uint Combine(uint crc1, uint crc2, int length2)
        {
            if (length2 <= 0) return crc1;
            if (crc1 == kInitial) return crc2;

            if (even_cache == null)
            {
                Prepare_even_odd_Cache();
            }

            uint[] even = CopyArray(even_cache);
            uint[] odd = CopyArray(odd_cache);

            crc1 = ~crc1;
            crc2 = ~crc2;

            var len2 = (uint) length2;

            // apply len2 zeros to crc1 (first square will put the operator for one
            // zero byte, eight zero bits, in even)
            do
            {
                // apply zeros operator for this bit of len2
                gf2_matrix_square(even, odd);

                if ((len2 & 1) != 0) crc1 = gf2_matrix_times(even, crc1);
                len2 >>= 1;

                if (len2 == 0) break;

                // another iteration of the loop with odd and even swapped
                gf2_matrix_square(odd, even);
                if ((len2 & 1) != 0) crc1 = gf2_matrix_times(odd, crc1);
                len2 >>= 1;
            } while (len2 != 0);

            crc1 ^= crc2;
            return ~crc1;
        }

        private static void Prepare_even_odd_Cache()
        {
            var even = new uint[32]; // even-power-of-two zeros operator
            var odd = new uint[32]; // odd-power-of-two zeros operator

            // put operator for one zero bit in odd
            odd[0] = kCrcPoly; // the CRC-32 polynomial
            for (int i = 1; i < 32; i++) odd[i] = 1U << (i - 1);

            // put operator for two zero bits in even
            gf2_matrix_square(even, odd);

            // put operator for four zero bits in odd
            gf2_matrix_square(odd, even);

            odd_cache = odd;
            even_cache = even;
        }

        /// <param name="matrix">will not be modified</param>
        private static uint gf2_matrix_times(uint[] matrix, uint vec)
        {
            uint sum = 0;
            int i = 0;
            while (vec != 0)
            {
                if ((vec & 1) != 0) sum ^= matrix[i];
                vec >>= 1;
                i++;
            }
            return sum;
        }

        /// <param name="square">this array will be modified!</param>
        /// <param name="mat">will not be modified</param>
        private static void gf2_matrix_square(uint[] square, uint[] mat)
        {
            for (int i = 0; i < 32; i++)
                square[i] = gf2_matrix_times(mat, mat[i]);
        }

        private static uint[] CopyArray(uint[] a)
        {
            var b = new uint[a.Length];
            Buffer.BlockCopy(a, 0, b, 0, a.Length*sizeof (uint));
            return b;
        }

        #endregion Combining

        private class Job
        {
            private readonly ParallelCRC accumulator;
            private readonly Job waitForJob;
            private ArraySegment<byte> data;

            private ManualResetEventSlim finished;

            public Job(ArraySegment<byte> data, ParallelCRC accumulator, Job waitForJob)
            {
                this.data = data;
                this.accumulator = accumulator;
                this.waitForJob = waitForJob;
                finished = new ManualResetEventSlim(false);
            }

            public void Do(object arg)
            {
                var crc = ProcessBlock(kInitial, data.Array, data.Offset, data.Count);
                if (waitForJob != null) waitForJob.WaitAndDispose();
                accumulator.value = Combine(accumulator.value, crc, data.Count);
                finished.Set();
            }

            public void WaitAndDispose()
            {
                finished.Wait();
                Dispose();
            }

            public void Dispose()
            {
                if (finished != null) finished.Dispose();
                finished = null;
            }
        }
    }
}
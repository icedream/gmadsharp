using System;
using System.IO;

namespace CRC32
{
    public class TraditionalCRC
    {
        private const uint kCrcPoly = 0xEDB88320;
        private const uint kInitial = 0xFFFFFFFF;
        private static readonly uint[] Table;

        static TraditionalCRC()
        {
            unchecked
            {
                Table = new uint[256];
                for (uint i = 0; i < 256; i++)
                {
                    uint r = i;
                    for (int j = 0; j < 8; j++)
                        r = (r >> 1) ^ (kCrcPoly & ~((r & 1) - 1));
                    Table[i] = r;
                }
            }
        }

        private uint value;

        public TraditionalCRC()
        {
            Init();
        }

        /// <summary>
        /// Reset CRC
        /// </summary>
        public void Init()
        {
            value = kInitial;
        }

        public int Value
        {
            get { return (int)~value; }
        }

        public void UpdateByte(byte b)
        {
            value = (value >> 8) ^ Table[(byte)value ^ b];
        }

        public void Update(byte[] data, int offset, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException("count");
            while (count-- != 0)
                value = (value >> 8) ^ Table[(byte)value ^ data[offset++]];
        }

        static public int Compute(byte[] data, int offset, int count)
        {
            var crc = new TraditionalCRC();
            crc.Update(data, offset, count);
            return crc.Value;
        }

        static public int Compute(byte[] data)
        {
            return Compute(data, 0, data.Length);
        }

        static public int Compute(ArraySegment<byte> block)
        {
            return Compute(block.Array, block.Offset, block.Count);
        }
    }
}
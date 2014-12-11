using System;

namespace GarrysMod.AddonCreator.Hashing
{
    public class TraditionalCRC
    {
        private const uint kCrcPoly = 0xEDB88320;
        private const uint kInitial = 0xFFFFFFFF;
        private static readonly uint[] Table;

        private uint value;

        static TraditionalCRC()
        {
            unchecked
            {
                Table = new uint[256];
                for (uint i = 0; i < 256; i++)
                {
                    var r = i;
                    for (var j = 0; j < 8; j++)
                        r = (r >> 1) ^ (kCrcPoly & ~((r & 1) - 1));
                    Table[i] = r;
                }
            }
        }

        public TraditionalCRC()
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
            if (count < 0) throw new ArgumentOutOfRangeException("count");
            while (count-- != 0)
                value = (value >> 8) ^ Table[(byte) value ^ data[offset++]];
        }

        public static int Compute(byte[] data, int offset, int count)
        {
            var crc = new TraditionalCRC();
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
    }
}
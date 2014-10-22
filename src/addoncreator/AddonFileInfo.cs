using CRC32;

namespace GarrysMod.AddonCreator
{
    public abstract class AddonFileInfo
    {
        private long? _size;
        private int? _hash;

        public virtual long Size { get { return _size.HasValue ? _size.Value : (_size = GetContents().Length).Value; } }

        public virtual int Crc32Hash { get { return _hash.HasValue ? _hash.Value : (_hash = ParallelCRC.Compute(GetContents())).Value; } }

        public abstract byte[] GetContents();
    }
}
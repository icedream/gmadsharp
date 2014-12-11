using System.Diagnostics;
using GarrysMod.AddonCreator.Hashing;

namespace GarrysMod.AddonCreator.Addon
{
    public abstract class AddonFileInfo
    {
        private int? _hash;
        private long? _size;

        /// <summary>
        ///     The file size.
        /// </summary>
        public virtual long Size
        {
            get
            {
                lock (this)
                {
                    if (_size != null)
                        return _size.Value;

                    var contents = GetContents();
                    Debug.Assert(contents != null, "Contents are NULL");

                    return (_size = contents.Length).Value;
                }
            }
        }

        /// <summary>
        ///     The CRC32 hash of this file.
        /// </summary>
        public virtual int Crc32Hash
        {
            get
            {
                lock (this)
                {
                    if (_hash != null)
                        return _hash.Value;

                    var contents = GetContents();
                    Debug.Assert(contents != null, "Contents are NULL");

                    return (_hash = Crc32.Compute(GetContents())).Value;
                }
            }
        }

        /// <summary>
        ///     Reads all contents of this file and returns it as a byte array.
        /// </summary>
        /// <returns>Byte array of the file content</returns>
        public abstract byte[] GetContents();
    }
}
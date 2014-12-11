using System;
using System.IO;

namespace GarrysMod.AddonCreator.Addon
{
    /// <summary>
    /// Represents an imported segment from another file.
    /// </summary>
    public class SegmentedAddonFileInfo : AddonFileInfo
    {
        private readonly int _hash;
        private readonly long _len;
        private readonly long _pos;
        private readonly Stream _stream;

        /// <summary>
        /// Constructs an instance of <see cref="SegmentedAddonFileInfo" /> using the given parameters.
        /// </summary>
        /// <param name="stream">The source stream from which to extract the file segment</param>
        /// <param name="pos">The offset from which to start reading</param>
        /// <param name="len">The length of the segment to read</param>
        /// <param name="fileHash">The CRC32 of the segment to read</param>
        internal SegmentedAddonFileInfo(Stream stream, long pos, long len, int fileHash)
        {
            _stream = stream;
            _pos = pos;
            _len = len;
            _hash = fileHash;
        }

        /// <summary>
        /// The file segment size.
        /// </summary>
        public override long Size
        {
            get { return _len; }
        }

        /// <summary>
        /// The file segment's CRC32 hash.
        /// </summary>
        public override int Crc32Hash
        {
            get { return _hash; }
        }

        /// <summary>
        /// Reads the complete segment and returns the content as a byte array.
        /// </summary>
        /// <returns>The content of the file segment</returns>
        public override byte[] GetContents()
        {
            lock (_stream)
            {
                var output = new byte[_len];
                var oldpos = _stream.Position;
                _stream.Position = _pos;
                for (long i = 0; i < _len; i += int.MaxValue) // for-loop for supporting long file sizes
                {
                    var toRead = (int) Math.Min(int.MaxValue, _len);
                    var buffer = new byte[toRead];
                    var readReal = _stream.Read(buffer, 0, toRead);
                    buffer.CopyTo(output, i);

                    i -= (toRead - readReal); // make absolutely sure everything gets read
                }
                _stream.Position = oldpos;
                return output;
            }
        }
    }
}
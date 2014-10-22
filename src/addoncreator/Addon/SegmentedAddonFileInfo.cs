using System;
using System.IO;

namespace GarrysMod.AddonCreator.Addon
{
    public class SegmentedAddonFileInfo : AddonFileInfo
    {
        private readonly long _len;
        private readonly long _pos;
        private readonly Stream _stream;
        private int _hash;

        public SegmentedAddonFileInfo(Stream stream, long pos, long len, int fileHash)
        {
            _stream = stream;
            _pos = pos;
            _len = len;
            _hash = fileHash;
        }

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
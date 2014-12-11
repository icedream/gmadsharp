using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TagLib;
using File = System.IO.File;

namespace GarrysMod.AddonCreator.Addon
{
    /// <summary>
    /// Represents a media addon file with the possibility of stripping tags and checking for corruptions.
    /// </summary>
    public class MinifiedMediaAddonFileInfo  : AddonFileInfo
    {
        private readonly string _tempFile;

        private bool _processed;

        ~MinifiedMediaAddonFileInfo()
        {
            File.Delete(_tempFile);
        }

        /// <summary>
        /// Creates a new <see cref="MinifiedMediaAddonFileInfo"/> instance using the given addon file.
        /// </summary>
        /// <param name="file">The addon file, supposedly a media file</param>
        public MinifiedMediaAddonFileInfo(AddonFileInfo file, string extension)
        {
            _tempFile = Path.GetTempFileName();
            var dirName = Path.GetDirectoryName(_tempFile);
            if (dirName == null)
                throw new InvalidOperationException("Temporary directory is NULL");

            // Fix extension, needed for TagLib to detect file format properly
            var newTempFile = Path.Combine(
                dirName,
                Path.GetFileNameWithoutExtension(_tempFile) + "." + extension);
            File.Move(_tempFile, newTempFile);
            _tempFile = newTempFile;

            using (var s = new FileStream(_tempFile, FileMode.OpenOrCreate, FileAccess.Write))
            {
                var buffer = file.GetContents();
                s.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// Indicates whether tags should be stripped or not.
        /// </summary>
        public bool StripTags { get; set; }

        /// <summary>
        /// Indicates whether to ignore possible file corruption.
        /// </summary>
        public bool IgnoreCorrupted { get; set; }

        /// <summary>
        /// Processes the media file, applies any wanted stripping and returns the new file contents.
        /// </summary>
        /// <returns>The contents of the media file with optional applied stripping</returns>
        /// <exception cref="CorruptFileException">Will be thrown when TagLib detects possible media file corruptions</exception>
        public override byte[] GetContents()
        {
            if (_processed)
            {
                using (var s = new FileStream(_tempFile, FileMode.Open, FileAccess.Read))
                {
                    var buffer = new byte[s.Length];
                    s.Read(buffer, 0, buffer.Length);
                    return buffer;
                }
            }

            using (var tags = TagLib.File.Create(_tempFile))
            {
                if (tags.PossiblyCorrupt && !IgnoreCorrupted)
                {
                    throw new CorruptFileException(string.Join("; ", tags.CorruptionReasons));
                }

                if (StripTags)
                {
                    tags.RemoveTags(TagTypes.AllTags);
                }

                tags.Save();
            }

            _processed = true;

            // This will now return the file contents instead
            return GetContents();
        }
    }
}

using System.IO;

namespace GarrysMod.AddonCreator.Addon
{
    /// <summary>
    /// Represents an addon file which exists on the harddisk as a separate file.
    /// </summary>
    public class PhysicalAddonFileInfo : AddonFileInfo
    {
        private readonly FileInfo _fi;

        /// <summary>
        /// Creates an instance of <see cref="PhysicalAddonFileInfo"/> from a given file path.
        /// </summary>
        /// <param name="path">The file path</param>
        public PhysicalAddonFileInfo(string path) : this(new FileInfo(path))
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="PhysicalAddonFileInfo"/> from given file information.
        /// </summary>
        /// <param name="file">The file info instance</param>
        public PhysicalAddonFileInfo(FileInfo file)
        {
            _fi = file;
        }

        /// <summary>
        /// The size of the file.
        /// </summary>
        public override long Size
        {
            get { return _fi.Length; }
        }

        /// <summary>
        /// Returns the file contents as a byte array.
        /// </summary>
        /// <returns>A byte array of the file content</returns>
        public override byte[] GetContents()
        {
            return File.ReadAllBytes(_fi.FullName);
        }
    }
}
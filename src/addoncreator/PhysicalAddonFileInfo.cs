using System.IO;

namespace GarrysMod.AddonCreator
{
    public class PhysicalAddonFileInfo : AddonFileInfo
    {
        public PhysicalAddonFileInfo(string path)
        {
            _fi = new FileInfo(path);
        }

        private FileInfo _fi;

        public override long Size
        {
            get { return _fi.Length; }
        }

        public override byte[] GetContents()
        {
            return File.ReadAllBytes(_fi.FullName);
        }
    }
}
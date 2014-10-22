using System.IO;

namespace GarrysMod.AddonCreator
{
    public class PhysicalAddonFileInfo : AddonFileInfo
    {
        private readonly FileInfo _fi;

        public PhysicalAddonFileInfo(string path)
        {
            _fi = new FileInfo(path);
        }

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
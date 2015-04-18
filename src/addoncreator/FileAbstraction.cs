using System.IO;
using File = TagLib.File;

namespace GarrysMod.AddonCreator
{
    class FileAbstraction : File.IFileAbstraction
    {
        private readonly string path;
        private readonly MemoryStream realWriteStream;
        public FileAbstraction(string path)
        {
            this.path = path;
            realWriteStream = new MemoryStream();
            WriteStream = new NonClosingStream(realWriteStream);
            ReadStream = new FileStream(path, FileMode.Open, FileAccess.Read);

            ReadStream.CopyTo(realWriteStream);
            ReadStream.Position = realWriteStream.Position = 0;

            Name = Path.GetFileName(path);
        }

        public string Name { get; private set; }

        public Stream ReadStream { get; private set; }

        public Stream WriteStream { get; private set; }

        public void CloseStream(Stream stream)
        {
            ReadStream.Dispose();

            if (!((NonClosingStream) WriteStream).Written)
                return;

            realWriteStream.Position = 0;
            using (var s = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                realWriteStream.CopyTo(s);
            }
            realWriteStream.Dispose();
        }
    }
}

using System;
using System.IO;
using System.Linq;
using GarrysMod.AddonCreator.Addon;

namespace GarrysMod.AddonCreator
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            switch (args[0])
            {
                case "create":
                {
                    var folder = new DirectoryInfo(args[1]);
                    var output = args[2];
                    var addon = new AddonFile();

                    // recursively add files
                    foreach (var file in folder.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        var relpath =
                            MakeRelativePath(folder.FullName, file.FullName).Replace(Path.DirectorySeparatorChar, '/');
                        Console.WriteLine("Adding: {0}", relpath);

                        addon.Files.Add(relpath, new PhysicalAddonFileInfo(file.FullName));
                    }

                    // create addon
                    Console.WriteLine("Exporting addon...");
                    addon.Export(output);

                    Console.WriteLine("Done.");
                    break;
                }
                case "extract":
                {
                    var gma = args[1];
                    var folder = new DirectoryInfo(args[2]);
                    var addon = new AddonFile();
                    addon.Import(gma);

                    Console.WriteLine("Loaded addon {0} by {1}, Version {2}", addon.Title, addon.Author, addon.Version);
                    Console.WriteLine("\t{0}", addon.Description);

                    // extract files
                    foreach (var file in addon.Files)
                    {
                        var relpath = file.Key;
                        var targetFile =
                            new FileInfo(Path.Combine(folder.FullName, relpath.Replace('/', Path.DirectorySeparatorChar)));

                        Console.WriteLine("Extracting: {0}", relpath);

                        // create directory
                        var dir = targetFile.Directory;
                        if (dir == null)
                            continue; // I still need to think about the weird logic here
                        dir.Create();

                        // create file
                        using (var fs = targetFile.Create())
                        {
                            var buffer = file.Value.GetContents();

                            // long-compatible copy algorithm
                            for (long i = 0; i < buffer.LongLength; i += int.MaxValue)
                            {
                                var toWrite = (int) Math.Min(int.MaxValue, buffer.LongLength - i);
                                var toWriteBuf = buffer.AsEnumerable();
                                for (long j = 0; j < i; j += int.MaxValue)
                                {
                                    toWriteBuf = toWriteBuf.Skip(int.MaxValue);
                                }
                                fs.Write(toWriteBuf.ToArray(), 0, toWrite);
                            }
                        }
                    }

                    Console.WriteLine("Done.");
                    break;
                }
            }
        }

        /// <summary>
        ///     Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private static String MakeRelativePath(String fromPath, String toPath)
        {
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            var fromUri = new Uri(fromPath + Path.DirectorySeparatorChar);
            var toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            } // path can't be made relative.

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.ToUpperInvariant() == "FILE")
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
    }
}
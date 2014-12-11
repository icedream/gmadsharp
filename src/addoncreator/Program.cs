using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using GarrysMod.AddonCreator.Addon;

namespace GarrysMod.AddonCreator
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var minimizeLua = false;
            var minimizeMedia = true;

            while (args.Any())
            {
                switch (args.Length == 0 ? "" : args[0])
                {
                    case "--version":
                        Console.WriteLine("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version);
                        args = args.Skip(1).ToArray();
                        break;
                    case "--minimize-lua":
                        minimizeLua = true;
                        args = args.Skip(1).ToArray();
                        break;
                    case "--no-minimize-lua":
                        minimizeLua = false;
                        args = args.Skip(1).ToArray();
                        break;
                    case "--minimize-media":
                        minimizeMedia = true;
                        args = args.Skip(1).ToArray();
                        break;
                    case "--no-minimize-media":
                        minimizeMedia = false;
                        args = args.Skip(1).ToArray();
                        break;
                    case "create":
                    {
                        if (args.Length < 3)
                        {
                            goto default;
                        }

                        var folder = new DirectoryInfo(args[1]);
                        var output = args[2];
                        var addon = new AddonFile {MinimizeLua = minimizeLua, MinimizeMedia = minimizeMedia};

                        if (!folder.Exists)
                        {
                            Console.Error.WriteLine(
                                "ERROR: Input folder needs to exist and needs to contain appropriate data.");
                            return;
                        }

                        // recursively add files
                        foreach (var file in folder.EnumerateFiles("*", SearchOption.AllDirectories))
                        {
                            var relpath =
                                MakeRelativePath(folder.FullName, file.FullName)
                                    .Replace(Path.DirectorySeparatorChar, '/');

                            addon.Files.Add(relpath, new PhysicalAddonFileInfo(file.FullName));
                        }

                        // create addon
                        Console.WriteLine("Exporting addon...");
                        addon.Export(output);

                        Console.WriteLine("Done.");
                        args = args.Skip(3).ToArray();
                        break;
                    }

                    case "extract":
                    {
                        if (args.Length < 3)
                        {
                            goto default;
                        }

                        var gma = args[1];
                        var folder = new DirectoryInfo(args[2]);

                        if (!File.Exists(gma))
                        {
                            Console.Error.WriteLine("ERROR: Input GMA file needs to exist.");
                            return;
                        }

                        var addon = new AddonFile();
                        try
                        {
                            addon.Import(gma);
                        }
                        catch (Exception err)
                        {
                            Console.Error.WriteLine("ERROR: Input GMA file could not be read - {0}", err.Message);
#if DEBUG
                            Debugger.Break();
#endif
                            return;
                        }

                        Console.WriteLine("## Addon information ##");
                        Console.WriteLine(addon.Title);
                        Console.WriteLine("\tVersion {0}", addon.Version);
                        Console.WriteLine("\tby {0}", addon.Author);
                        Console.WriteLine();
                        Console.WriteLine(addon.Description);
                        Console.WriteLine();

                        // extract files
                        foreach (var file in addon.Files)
                        {
                            var relpath = file.Key.Replace(Path.DirectorySeparatorChar, '/');
                            var targetFile =
                                new FileInfo(Path.Combine(folder.FullName,
                                    relpath.Replace('/', Path.DirectorySeparatorChar)));

                            Console.WriteLine("Extracting: {0}", relpath);

                            // create directory
                            var dir = targetFile.Directory;
                            if (dir == null || relpath.Contains("../"))
                                continue; // relative path trying to be sneaky here
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

                                fs.Flush();
                            }
                        }

                        Console.WriteLine("Done.");
                        args = args.Skip(3).ToArray();
                        break;
                    }

                    default:
                        Console.WriteLine("Usage: {0} <options> <command> <arguments>",
                            Process.GetCurrentProcess().ProcessName);
                        Console.WriteLine();
                        Console.WriteLine("Commands:");
                        Console.WriteLine("\t{0}\t{1}", "extract", "Extracts a GMA file and shows information about it.");
                        Console.WriteLine("\t\tArguments: Input GMA file path, output folder path");
                        Console.WriteLine("\t{0}\t{1}", "create", "Creates a GMA file.");
                        Console.WriteLine("\t\tArguments: Input folder path, output GMA file path");
                        Console.WriteLine();
                        Console.WriteLine("Options:");
                        Console.WriteLine("\t{0}\t{1}", "--minimize-lua",
                            "Causes exported GMAs to have all Lua comments and unneeded whitespace in Lua stripped out.");
                        Console.WriteLine("\t{0}\t{1}", "--no-minimize-lua",
                            "(default) Will prevent Lua files getting minimized.");
                        Console.WriteLine("\t{0}\t{1}", "--minimize-media",
                            "(default) Causes exported GMAs to have all media tags stripped out.");
                        Console.WriteLine("\t{0}\t{1}", "--no-minimize-media",
                            "Will prevent media files getting minimized.");
                        Console.WriteLine();
                        return;
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
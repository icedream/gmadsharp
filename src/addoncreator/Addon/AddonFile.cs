using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using GarrysMod.AddonCreator.Hashing;
using Newtonsoft.Json;

namespace GarrysMod.AddonCreator.Addon
{
    public class AddonFile
    {
        private const byte FormatVersion = 3;
        private const uint AppID = 4000;
        private const uint CompressionSignature = 0xbeefcace;
        private static readonly byte[] FormatIdent = Encoding.ASCII.GetBytes("GMAD");

        /// <summary>
        ///     Initializes a new instance of <see cref="AddonFile" />
        /// </summary>
        public AddonFile()
        {
            Files = new Dictionary<string, AddonFileInfo>();
            RequiredContent = new List<string>();
            Version = 1;
        }

        /// <summary>
        ///     Returns the timestamp of when the addon was built. This data is retrieved from full imports and for new (unsaved)
        ///     addons this is 0.
        /// </summary>
        public ulong BuildTimestamp { get; private set; }

        /// <summary>
        ///     The name of this addon.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///     The author of this addon.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        ///     A description of this addon.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     This addon's version.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        ///     The files to include in the addon.
        /// </summary>
        public Dictionary<string, AddonFileInfo> Files { get; private set; }

        /// <summary>
        ///     Currently unused.
        /// </summary>
        public ulong SteamID { get; set; }

        /// <summary>
        ///     Content that needs to exist in order to run this addon.
        /// </summary>
        public List<string> RequiredContent { get; private set; }

        /// <summary>
        ///     Imports a gmod addon into this instance.
        /// </summary>
        /// <param name="path">Path to a gmod addon file.</param>
        /// <param name="withMetadata">Import all metadata (title, description, creator, timestamp, etc.) as well?</param>
        public void Import(string path, bool withMetadata = true)
        {
            var stream = File.OpenRead(path);

            {
                var sr = new BinaryReader(stream, Encoding.GetEncoding("iso-8859-1"));

                // Check format header
                if (!sr.ReadBytes(4).SequenceEqual(FormatIdent)
                    || sr.ReadByte() != FormatVersion)
                    throw new FormatException();

#if DEBUG
                // Check addon's CRC32 hash
                // TODO: Garry's code actually calculates CRC32 hashes differently in edge cases. The code used for it is from zlib-1.1.3. See https://github.com/garrynewman/bootil/blob/master/src/3rdParty/smhasher/crc.cpp#L4.
                {
                    Debug.WriteLine("Checking CRC32...");
                    var baseAddon = new byte[stream.Length - sizeof (int)];
                    var oldpos = stream.Position;
                    stream.Position = 0;
                    stream.Read(baseAddon, 0, baseAddon.Length);
                    var baseAddonHash = sr.ReadInt32();
                    var calcAddonHash = Crc32.Compute(baseAddon);
                    Debug.WriteLine("\tCalculated hash: {0}; Wanted hash: {1}", calcAddonHash, baseAddonHash);
                    Debug.Assert(calcAddonHash == baseAddonHash, "CRC32 hash mismatch",
                        "Calculated CRC32 hash is different from the one saved within the addon. Causes could be a corrupted file or the edge case bug https://github.com/icedream/gmadsharp/issues/2.");
                    stream.Position = oldpos;
                }
#endif

                // Import metadata
                var newSteamID = sr.ReadUInt64();
                var newBuildTimestamp = sr.ReadUInt64();
                var newRequiredContentLen = sr.ReadByte();
                var newTitle = sr.ReadString(true);
                var newDescription = sr.ReadString(true);
                var newAuthor = sr.ReadString(true);
                var newVersion = sr.ReadInt32();

                Debug.WriteLine("## Metadata ##");
                Debug.WriteLine("Steam ID: {0}", newSteamID);
                Debug.WriteLine("Build time: {0}", newBuildTimestamp);
                Debug.WriteLine("Required content count: {0}", newRequiredContentLen);

                Debug.Assert(newSteamID == 0);
                Debug.Assert(newRequiredContentLen == 0);

                for (var b = 0; b < newRequiredContentLen; b++)
                {
                    var value = sr.ReadString(true);
                    if (withMetadata && !RequiredContent.Contains(value))
                        RequiredContent.Add(value);
                }

                if (withMetadata)
                {
                    SteamID = newSteamID;
                    BuildTimestamp = newBuildTimestamp;
                    Title = newTitle;
                    Description = newDescription;
                    Author = newAuthor;
                    Version = newVersion;
                }

                Debug.WriteLine("");

                // file list
                Debug.WriteLine("## File list ##");
                var newFilesList = new Dictionary<string, Tuple<long, int>>();
                var expectedFileId = 1;
                do
                {
                    var fileId = sr.ReadUInt32();
                    if (fileId == 0)
                        break; // end of list

                    // key, size, hash
                    var filePath = sr.ReadString(true);
                    var fileSize = sr.ReadInt64();
                    var fileHash = sr.ReadInt32();

                    Debug.WriteLine("\t#{2} : {0} ({1:0.0} kB)", filePath, (double)fileSize/1024, fileId);
                    Debug.Assert(fileId == expectedFileId);

                    expectedFileId++;

                    // avoid duplicates
                    if (newFilesList.ContainsKey(filePath))
                    {
                        throw new IOException(
                            "Found duplicate file path in addon file. Contact the addon creator and tell him to build a new proper addon file.");
                    }

                    newFilesList.Add(filePath, new Tuple<long, int>(fileSize, fileHash));
                } while (true);
                Debug.WriteLine("");

                Debug.WriteLine("## File import ##");
                foreach (var file in newFilesList)
                {
                    var filePath = file.Key;
                    var fileSize = file.Value.Item1;
                    var fileHash = file.Value.Item2;
                    var filePosition = sr.BaseStream.Position;

                    Debug.WriteLine("Analyzing: {0} ({1:0.00} kB)", filePath, (double)fileSize / 1024);

                    var fileContent = new byte[fileSize];

                    // long-compatible file reading
                    for (long i = 0; i < fileSize; i += int.MaxValue)
                    {
                        var tempContent = sr.ReadBytes((int) Math.Min(int.MaxValue, fileSize));
                        tempContent.CopyTo(fileContent, i);
                    }

                    // CRC check for this file
                    var fileCalcHash = Crc32.Compute(fileContent);
                    Debug.WriteLine("\t\tCalculated hash: {0}; Wanted hash: {1}", fileCalcHash, fileHash);
                    if (fileCalcHash != fileHash)
                    {
                        throw new IOException("File " + filePath + " in addon file is corrupted (hash mismatch)");
                    }

                    Files.Add(filePath, new SegmentedAddonFileInfo(stream, filePosition, fileSize, fileHash));
                }
            }
        }

        /// <summary>
        ///     Exports this addon into a GMA file.
        /// </summary>
        /// <param name="path">The output file path, should be pointing to a writable location ending with ".gma".</param>
        public void Export(string path)
        {
            // Enforce .gma extension
            if (!path.EndsWith(".gma", StringComparison.OrdinalIgnoreCase))
            {
                var pathSplit = path.Split('.');
                pathSplit[pathSplit.Length - 1] = "gma";
                path = string.Join(".", pathSplit);
            }

            // Checking for existing addon.json
            if (!Files.ContainsKey("addon.json"))
            {
                throw new FileNotFoundException("Addon building requires a valid addon.json file.");
            }

            var files = Files;

            // Check for errors and ignores in addon.json
            var addonJson =
                JsonConvert.DeserializeObject<AddonJson>(Encoding.UTF8.GetString(Files["addon.json"].GetContents()));
            addonJson.CheckForErrors();
            addonJson.RemoveIgnoredFiles(ref files);

            // Extract data from addon.json
            Title = addonJson.Title;
            Author = addonJson.Author;
            Description = string.IsNullOrEmpty(addonJson.Description) ? string.Empty : addonJson.Description;
            Version = addonJson.Version;

            // Create a stripped down version of addon.json for the output gma, it will replace the Description field
            var newDescription = JsonConvert.SerializeObject(new AddonJson
            {
                Description = Description,
                Tags = addonJson.Tags,
                Type = addonJson.Type
            });

            // Sort files
            var resultingFiles = new SortedDictionary<string, AddonFileInfo>(files);
            resultingFiles.Remove("addon.json");

            // General whitelist
            var blacklistedFiles = AddonWhitelist
                .FindBlacklistedFiles(resultingFiles.Select(i => i.Key))
                .ToArray();
            if (blacklistedFiles.Any())
            {
                throw new InvalidOperationException("Found files which aren't whitelisted. Remove or ignore those files before you retry packing your addon:"
                                                    + Environment.NewLine + Environment.NewLine
                                                    + string.Join(Environment.NewLine, blacklistedFiles));
            }

            using (var stream = new MemoryStream())
            {
                // TODO: Standardized encoding - Garry should use standardized encoding, currently he uses Encoding.Default which is applocale-dependent...
                var sw = new BinaryWriter(stream, Encoding.GetEncoding("iso-8859-1"));

                // Format header
                sw.Write(FormatIdent);
                sw.Write(FormatVersion);

                // Creator steam ID
                sw.Write(SteamID);

                // Build timestamp
                sw.Write(BuildTimestamp = (ulong) (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);

                // Required content
                if (RequiredContent.Count > byte.MaxValue)
                {
                    throw new IndexOutOfRangeException("Required content count must not exceed " + byte.MaxValue +
                                                       " entries.");
                }
                sw.Write((byte) RequiredContent.Count);
                foreach (var content in RequiredContent)
                {
                    sw.Write(content, true);
                }

                // Metadata
                sw.Write(Title, true);
                sw.Write(newDescription, true); // "Description" field which is actually a trimmed down addon.json
                sw.Write(Author, true);
                sw.Write(Version);

                // File list
#if SUPPORT_BIG
                if (Files.Count > uint.MaxValue)
                {
                    throw new IndexOutOfRangeException("Number of addon files must not exceed " + uint.MaxValue +
                                                       " elements.");
                }
#endif
                uint fileNum = 0;
                foreach (var file in resultingFiles)
                {
                    Console.WriteLine("Processing: {0} ({1:0.00} kB)", file.Key, (double)file.Value.Size / 1024);

                    fileNum++;
                    sw.Write(fileNum);
                    sw.Write(file.Key.ToLower(), true); // Path
                    sw.Write(file.Value.Size);
                    sw.Write(file.Value.Crc32Hash);
                }
                sw.Write((uint) 0); // End of file list

                // File contents
                foreach (var file in resultingFiles)
                {
                    if (file.Value.Size == 0)
                        continue;

                    Console.WriteLine("Packing: {0}", file.Key);

                    sw.Write(file.Value.GetContents());
                }

                // Addon CRC
                var addonHash = Crc32.Compute(stream.ToArray());
                sw.Write(addonHash);

                using (var outfile = File.Create(path))
                {
                    stream.Position = 0;
                    stream.CopyTo(outfile);
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace GarrysMod.AddonCreator.Addon
{
    /// <summary>
    /// Represents information about an addon.
    /// </summary>
    public class AddonJson
    {
        /// <summary>
        /// Creates an instance of <see cref="AddonJson"/>.
        /// </summary>
        public AddonJson()
        {
            Version = 1;
        }

        /// <summary>
        /// The title of the addon.
        /// </summary>
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        /// <summary>
        /// A description of the addon.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// The type of the addon.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// The assigned tags of the addon.
        /// </summary>
        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        /// <summary>
        /// A list of patterns of files to ignore when exporting the addon.
        /// </summary>
        [JsonProperty("ignore", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Ignores { get; set; }

        /// <summary>
        /// The addon's version.
        /// </summary>
        [JsonProperty("version")]
        public int Version { get; set; }

        /// <summary>
        /// The author of the addon.
        /// </summary>
        [JsonProperty("author")]
        public string Author { get; set; }

        /// <summary>
        /// Validates the addon information for mistakes. This includes missing/empty title, missing/empty/invalid description and if the type is missing/empty.
        /// </summary>
        internal void CheckForErrors()
        {
            if (string.IsNullOrEmpty(Title))
            {
                throw new MissingFieldException("Title is empty or not specified.");
            }

            if (!string.IsNullOrEmpty(Description) && Description.Contains('\0'))
            {
                throw new InvalidDataException("Description contains NULL character.");
            }

            if (string.IsNullOrEmpty(Type))
            {
                throw new MissingFieldException("Type is empty or not specified.");
            }

            // TODO: Validate tags using a predefined list.
        }

        /// <summary>
        /// Removes files matching any of the ignore patterns from a prepared file dictionary.
        /// </summary>
        /// <param name="files">The file dictionary to scan for files to remove</param>
        public void RemoveIgnoredFiles(ref Dictionary<string, AddonFileInfo> files)
        {
            foreach (var key in files.Keys.ToArray())
                // ToArray makes a shadow copy of Keys to avoid "mid-loop-removal" conflicts
            {
                if (Ignores.Any(w => w.WildcardRegex().IsMatch(key)))
                    files.Remove(key);
            }
        }
    }
}
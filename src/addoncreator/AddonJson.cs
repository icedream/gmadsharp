using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace GarrysMod.AddonCreator
{
    public class AddonJson
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("ignore")]
        public List<string> Ignores { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

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
        }

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

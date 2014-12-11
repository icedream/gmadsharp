using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GarrysMod.AddonCreator.Addon
{
    /// <summary>
    /// Handles file whitelisting.
    /// </summary>
    public static class AddonWhitelist
    {
        /// <summary>
        /// Contains all allowed file patterns.
        /// </summary>
        private static readonly string[] Whitelist =
        {
            "addon.json",
            "lua/*.lua",
            "scenes/*.vcd",
            "particles/*.pcf",
            "resource/fonts/*.ttf",
            "scripts/vehicles/*.txt",
            "resource/localization/*/*.properties",
            "maps/*.bsp",
            "maps/*.nav",
            "maps/*.ain",
            "maps/thumb/*.png",
            "sound/*.wav",
            "sound/*.mp3",
            "sound/*.ogg",
            "materials/*.vmt",
            "materials/*.vtf",
            "materials/*.png",
            "materials/*.jpg",
            "materials/*.jpeg",
            "models/*.mdl",
            "models/*.vtx",
            "models/*.phy",
            "models/*.ani",
            "models/*.vvd",
            "gamemodes/*/*.txt",
            "gamemodes/*/*.fgd",
            "gamemodes/*/logo.png",
            "gamemodes/*/icon24.png",
            "gamemodes/*/gamemode/*.lua",
            "gamemodes/*/entities/effects/*.lua",
            "gamemodes/*/entities/weapons/*.lua",
            "gamemodes/*/entities/entities/*.lua",
            "gamemodes/*/backgrounds/*.png",
            "gamemodes/*/backgrounds/*.jpg",
            "gamemodes/*/backgrounds/*.jpeg",
            "gamemodes/*/content/models/*.mdl",
            "gamemodes/*/content/models/*.vtx",
            "gamemodes/*/content/models/*.phy",
            "gamemodes/*/content/models/*.ani",
            "gamemodes/*/content/models/*.vvd",
            "gamemodes/*/content/materials/*.vmt",
            "gamemodes/*/content/materials/*.vtf",
            "gamemodes/*/content/materials/*.png",
            "gamemodes/*/content/materials/*.jpg",
            "gamemodes/*/content/materials/*.jpeg",
            "gamemodes/*/content/scenes/*.vcd",
            "gamemodes/*/content/particles/*.pcf",
            "gamemodes/*/content/resource/fonts/*.ttf",
            "gamemodes/*/content/scripts/vehicles/*.txt",
            "gamemodes/*/content/resource/localization/*/*.properties",
            "gamemodes/*/content/maps/*.bsp",
            "gamemodes/*/content/maps/*.nav",
            "gamemodes/*/content/maps/*.ain",
            "gamemodes/*/content/maps/thumb/*.png",
            "gamemodes/*/content/sound/*.wav",
            "gamemodes/*/content/sound/*.mp3",
            "gamemodes/*/content/sound/*.ogg"
        };

        private static Regex[] _regularExpressions;

        private static void ConvertWhitelist()
        {
            if (_regularExpressions != null)
                return;

            _regularExpressions = Whitelist.Select(w => w.WildcardRegex()).ToArray();
        }

        /// <summary>
        /// Scans a list of file paths for files which are not whitelisted and returns the found file paths.
        /// </summary>
        /// <param name="files">The list of file paths to scan</param>
        /// <returns>Found files which are not whitelisted</returns>
        public static IEnumerable<string> FindBlacklistedFiles(IEnumerable<string> files)
        {
            ConvertWhitelist();
            return files.Where(f => !_regularExpressions.Any(rx => rx.IsMatch(f)));
        }
    }
}
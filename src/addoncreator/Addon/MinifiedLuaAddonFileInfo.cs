using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GarrysMod.AddonCreator.Addon
{
    /// <summary>
    ///     Wrapper around another file info for optimizing Lua files.
    /// </summary>
    public class MinifiedLuaAddonFileInfo : AddonFileInfo
    {
        private readonly AddonFileInfo _fi;

        private byte[] _content;

        /// <summary>
        /// Contains regex which helps stripping out comments (and unnecessary whitespace lines)
        /// </summary>
        private readonly string _stripCommentsEmptylineRegex = string.Join("|", new[]
        {
            // block comments
            @"\/\*.*?\*\/",
            @"\-\-\[\[.*?\]\]",

            // line comments
            @"//([^\r\n]*?)(?<linebreak>[\r\n]+)",
            @"\-\-([^\r\n]*?)(?<linebreak>[\r\n]+)",

            // Unnecessary whitespace
            @"^[\s]*$",
            @"[\s]*$",
            @"^[\s]*"
        });

        private string _luaCode;

        public MinifiedLuaAddonFileInfo(AddonFileInfo actual)
        {
            _fi = actual;
        }

        public override byte[] GetContents()
        {
            if (_content != null)
                return _content;

            _luaCode = Encoding.UTF8.GetString(_fi.GetContents());

            // Remove comments and whitespace lines
            string oldLuaCode;
            do
            {
                oldLuaCode = _luaCode;
                _luaCode = Regex.Replace(_luaCode, _stripCommentsEmptylineRegex,
                    m => m.Groups["linebreak"] != null ? m.Groups["linebreak"].Value : "", RegexOptions.Multiline | RegexOptions.Singleline);
                _luaCode = _luaCode.Trim();
            } while (oldLuaCode != _luaCode);
 
            _content = Encoding.UTF8.GetBytes(_luaCode);

            return _content;
        }
        
    }
}
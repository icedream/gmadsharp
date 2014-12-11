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
    public class LuaAddonFileInfo : AddonFileInfo
    {
        private readonly AddonFileInfo _fi;

        private byte[] _content;

        private readonly string _stripCommentsRegex = string.Join("|", new[]
        {
            // block comments
            @"/\*(.*?)\*/",
            @"\-\-\[\[(.*?)\]\]",

            // line comments
            @"//(.*?)(?<linebreak>\r?\n)",
            @"\-\-(.*?)(?<linebreak>\r?\n)"
        });
        private string _luaCode;

        public LuaAddonFileInfo(AddonFileInfo actual)
        {
            _fi = actual;
        }

        public override byte[] GetContents()
        {
            if (_content != null)
                return _content;

            _luaCode = Encoding.UTF8.GetString(_fi.GetContents());
            _luaCode = Regex.Replace(_luaCode, _stripCommentsRegex, m => m.Groups["linebreak"] != null ? m.Groups["linebreak"].Value : "");


            return _content;
        }
        
    }
}
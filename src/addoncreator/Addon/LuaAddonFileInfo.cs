using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if LUA_BYTECODE
using GarrysMod.AddonCreator.Compiler;
using SharpLua;
using SharpLua.Visitors;
#endif

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

#if LUA_BYTECODE
            GenerateBytecode();
#else
            _content = Encoding.UTF8.GetBytes(_luaCode);
#endif

            return _content;
        }
        
#if LUA_BYTECODE
        private void GenerateBytecode()
        {
            var lua = Lua.lua_open();

            if (Lua.lua_cpcall(lua, LuaMain, null) != 0)
                throw new Exception();

            Lua.lua_close(lua);

            Debug.WriteLine("Bytecode generated ({0} bytes)", _content.Length);
            if (_content.Length == 0)
            {
                Debugger.Break();
            }
        }

        private int LuaMain(Lua.LuaState lua)
        {
            try
            {
                var res = Lua.luaL_loadstring(lua, _luaCode);
                if (res != 0)
                {
                    Console.Error.WriteLine("ERROR while loading Lua code");
                    return -1;
                }

                using (var ms = new MemoryStream())
                {
                    Lua.lua_lock(lua);
                    Lua.luaU_dump(lua, Lua.clvalue(lua.top - 1).l.p, LuaWriter, ms, 1);
                    Lua.lua_unlock(lua);

                    _content = ms.ToArray();
                }

                return 0;
            }
            catch (LuaSourceException error)
            {
                Console.Error.WriteLine("PARSE ERROR - Line {1}, col {2}: {0}", error.Message, error.Line, error.Column);
                File.WriteAllText("generated.lua", _luaCode);
                return -1;
            }
            catch (LuaScriptException error)
            {
                Console.Error.WriteLine("LUA ERROR - {1}: {0}", error.Message, error.Source);
                File.WriteAllText("generated.lua", _luaCode);
                return -1;
            }
        }

        static int LuaWriter(Lua.LuaState lua, Lua.CharPtr ptr, uint size, object targetStream)
        {
            return ((Lua.fwrite(ptr, (int)size, 1, (Stream)targetStream) != 1) && (size != 0)) ? 1 : 0;
        }
#endif
    }
}
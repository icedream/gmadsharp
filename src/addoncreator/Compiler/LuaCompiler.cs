#if LUA_BYTECODE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using KopiLua;

namespace GarrysMod.AddonCreator.Compiler
{
    public static class LuaCompiler
    {
        public static byte[] Compile(string luaCode)
        {
            var lua = new NLua.Lua();

            var b = new List<byte>();

            var meth = new Func<string>(() => luaCode);
            lua.LoadString(luaCode, "luaCode");
            lua.RegisterFunction("appendBytecode", b, b.GetType().GetMethod("Add"));
            lua.RegisterFunction("appendBytecode", b, b.GetType().GetMethod("Add"));

            lua.DoString("for b in string.gmatch(string.dump((luaCode),true),\".\") do appendBytecode(string.byte(b)) end");

            return b.ToArray();
        }
    }
}
#endif
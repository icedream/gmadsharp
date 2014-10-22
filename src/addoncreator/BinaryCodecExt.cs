using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GarrysMod.AddonCreator
{
    static class BinaryCodecExt
    {
        public static string ReadString(this BinaryReader br, bool nullTerminated)
        {
            if (!nullTerminated)
                return br.ReadString();

            var sb = new StringBuilder();
            do
            {
                var c = br.ReadChar();
                if (c == 0)
                    break;
                sb.Append(c);
            } while (true);
            return sb.ToString();
        }

        public static void Write(this BinaryWriter bw, string value, bool nullTerminated)
        {
            if (nullTerminated)
            {
                value += "\0";
            }

            bw.Write(value);
        }
    }
}

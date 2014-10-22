using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GarrysMod.AddonCreator
{
    internal static class Extensions
    {
        public static Regex WildcardRegex(this string pattern)
        {
            return new Regex("^" + Regex.Escape(pattern)
                .Replace(@"\*", ".*")
                .Replace(@"\?", ".")
                             + "$");
        }

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
            if (!nullTerminated)
            {
                bw.Write(value);
            }

            value += "\0";
            bw.Write(Encoding.GetEncoding("windows-1252").GetBytes(value));
        }
    }
}
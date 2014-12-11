using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GarrysMod.AddonCreator
{
    internal static class Extensions
    {
        /// <summary>
        /// Generates a regular expression from a wildcard.
        /// </summary>
        /// <param name="pattern">The wildcard pattern</param>
        /// <returns>A regular expression of the given pattern</returns>
        public static Regex WildcardRegex(this string pattern)
        {
            return new Regex("^" + Regex.Escape(pattern)
                .Replace(@"\*", ".*")
                .Replace(@"\?", ".")
                             + "$");
        }

        /// <summary>
        /// Reads a string. If nullTerminated is true, this will not use native string reading but reads until a NULL char is found. 
        /// </summary>
        /// <param name="br">The binary reader instance</param>
        /// <param name="nullTerminated">Use null-terminated reading instead of native string reading</param>
        /// <returns>The read string</returns>
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

        /// <summary>
        /// Writes a string. If nullTerminated is true, the string will not be written using native string writing but without length prefix and with NULL char termination instead.
        /// </summary>
        /// <param name="bw">The binary writer instance</param>
        /// <param name="value">The string to write</param>
        /// <param name="nullTerminated">Use null-terminated writing instead of native string writing</param>
        public static void Write(this BinaryWriter bw, string value, bool nullTerminated)
        {
            if (!nullTerminated)
            {
                bw.Write(value);
            }

            value += "\0";
            bw.Write(Encoding.GetEncoding("iso-8859-1").GetBytes(value));
        }
    }
}
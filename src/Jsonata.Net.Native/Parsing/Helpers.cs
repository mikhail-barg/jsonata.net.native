using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Parsing
{
    public static class Helpers
    {
        public static string JoinNodes(this List<Node> nodes, string separator) 
        {
            return String.Join(separator, nodes.Select(n => n.ToString()));
        }

        // unescape replaces JSON escape sequences in a string with their
        // unescaped equivalents. Valid escape sequences are:
        //
        // \X, where X is a character from jsonEscapes
        // \uXXXX, where XXXX is a 4-digit hexadecimal Unicode code point.
        //
        // unescape returns the unescaped string and true if successful,
        // otherwise it returns the invalid escape sequence and false.
        public static string Unescape(string src)
        {
            //https://stackoverflow.com/a/54355440/376066
            return System.Text.RegularExpressions.Regex.Unescape(src);
        }
    }
}

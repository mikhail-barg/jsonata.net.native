using Newtonsoft.Json;
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
        //https://stackoverflow.com/a/54354993/376066
        public static string JsonPrettify(string json, Formatting formatting = Formatting.Indented)
        {
            using (StringReader stringReader = new StringReader(json))
            using (StringWriter stringWriter = new StringWriter())
            {
                return JsonPrettify(stringReader, stringWriter, formatting).ToString()!;
            }
        }

        public static TextWriter JsonPrettify(TextReader textReader, TextWriter textWriter, Formatting formatting = Formatting.Indented)
        {
            // Let caller who allocated the the incoming readers and writers dispose them also
            // Disable date recognition since we're just reformatting
            using (JsonTextReader jsonReader = new JsonTextReader(textReader) { DateParseHandling = DateParseHandling.None, CloseInput = false })
            using (JsonTextWriter jsonWriter = new JsonTextWriter(textWriter) { Formatting = formatting, CloseOutput = false })
            {
                jsonWriter.WriteToken(jsonReader);
            }
            return textWriter;
        }

        public static string JoinNodes(List<Node> nodes, string separator) 
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

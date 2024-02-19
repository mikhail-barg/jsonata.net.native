using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    public sealed class JsonParseException: Exception
    {
        public int Position { get; }

        internal JsonParseException(JsonParser parser, string message) 
            : base($"Parsing failed: {message} (at position {parser.CurrentPosition})")
        {
            this.Position = parser.CurrentPosition;
        }
    }
}

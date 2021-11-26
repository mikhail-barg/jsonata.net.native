using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Parsing
{
    internal class Token
    {
        internal readonly TokenType type;
        internal readonly string? value;
        internal readonly int position;
        internal Token(TokenType type, string? value, int position)
        {
            this.type = type;
            this.value = value;
            this.position = position;
        }
    }

    internal sealed class RegexToken: Token
    {
        internal RegexOptions flags;

        internal RegexToken(string value, int position)
            :base(TokenType.typeRegex, value, position)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Parsing
{
    internal sealed record Token(
        TokenType type, 
        string? value, 
        int position
    )
    {
    }
}

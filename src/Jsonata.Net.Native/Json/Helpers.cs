using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    internal static class Helpers
    {
        internal static void Indent(this StringBuilder builder, int indent)
        {
            for (int i = 0; i < indent * 2; ++i)
            {
                builder.Append(' ');
            }
        }

        internal static void AppendJsonLine(this StringBuilder builder)
        {
            builder.Append('\n');
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    internal enum JTokenType
    {
        Object,
        Array,
        Integer,
        Float,
        String,
        Boolean,
        Null,
        Undefined,

        Function,
    }
}

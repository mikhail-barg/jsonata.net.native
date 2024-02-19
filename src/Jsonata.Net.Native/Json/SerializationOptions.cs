using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    public sealed class SerializationOptions
    {
        internal static readonly SerializationOptions Default = new SerializationOptions();

        public bool SerializeNullProperties { get; set; } = true;
    }
}

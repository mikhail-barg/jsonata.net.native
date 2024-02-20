using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    public sealed class SerializationSettings
    {
        internal static readonly SerializationSettings DefaultSettings = new SerializationSettings();

        public bool SerializeNullProperties { get; set; } = true;
    }
}

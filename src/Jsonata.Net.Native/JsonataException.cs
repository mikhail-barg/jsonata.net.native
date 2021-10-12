using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native
{
    public sealed class JsonataException: Exception
    {
        public string Code { get; }

        public JsonataException(string code, string message)
            : base($"{code}: {message}")
        {
            this.Code = code;
        }
    }
}

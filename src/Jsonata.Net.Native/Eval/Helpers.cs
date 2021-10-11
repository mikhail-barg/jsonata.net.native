using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    internal static class Helpers
    {
        public static void AddRange(this JArray array, IEnumerable<JToken> values)
        {
            foreach (JToken value in values)
            {
                array.Add(value);
            }
        }
    }
}

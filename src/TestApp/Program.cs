using Jsonata.Net.Native;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;

namespace TestApp
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            JsonataQuery query = new JsonataQuery("$.a");

            //from string
            {
                string result = query.Eval("{\"a\": \"b\"}");
                Debug.Assert(result == "\"b\"");
            }

            //from Json.Net
            {
                JToken data = JToken.Parse("{\"a\": \"b\"}");
                JToken result = query.Eval(data);
                Debug.Assert(result.ToString(Formatting.None) == "\"b\"");
            }
        }
    }
}

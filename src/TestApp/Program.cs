using Jsonata.Net.Native;
using Jsonata.Net.Native.Json;
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
                Check(result, "\"b\"");
            }

            //from Json.Net
            {
                JToken data = JToken.Parse("{\"a\": \"b\"}");
                JToken result = query.Eval(data);
                Check(result, "\"b\"");
            }

            //with bindings
            {
                JToken data = JToken.Parse("{\"a\": \"b\"}");

                JObject bindings = (JObject)JToken.Parse("{\"x\": \"y\"}");

                JsonataQuery query2 = new JsonataQuery("{'a': $.a, 'x': $x}");

                JToken result = query2.Eval(data, bindings);
                Check(result, "{\"a\":\"b\",\"x\":\"y\"}");
            }

            //with custom environment and function binding
            {
                JToken data = JToken.Parse("{\"a\": \"b\"}");

                JObject bindings = (JObject)JToken.Parse("{\"x\": \"y\"}");
                EvaluationEnvironment env = new EvaluationEnvironment(bindings);
                env.BindFunction(typeof(Program).GetMethod(nameof(foo)));

                JsonataQuery query2 = new JsonataQuery("{'a': $.a, 'x': $x, 'z': $foo()}");

                JToken result = query2.Eval(data, env);
                Check(result, "{\"a\":\"b\",\"x\":\"y\",\"z\":\"bar\"}");
            }
        }

        private static void Check(string value, string expected)
        {
            Console.WriteLine($"Expected: {expected}, got {value}");
            if (expected != value)
            {
                throw new Exception("Check failed");
            }
        }

        private static void Check(JToken value, string expected)
        {
            Check(value.ToFlatString(), expected);
        }

        public static string foo()
        {
            return "bar";
        }
    }
}

using System;
using System.IO;

namespace Jsonata.Net.Native
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            /*
            JsonataExpression query = new JsonataExpression("nest0.nest1[0]");
            string json = File.ReadAllText(@"f:\Projects-misc\jsonata-js\test\test-suite\datasets\dataset4.json");
            string result = query.Eval(json);
            Console.WriteLine("Hello World!");
            */

            JsonataQuery query = new JsonataQuery("*.a");
            string json = "[{'a': 'b'}, { 'c': {'a': 'd'}}, {'a': 'e'}]";
            string result = query.Eval(json);
            Console.WriteLine(result);
        }
    }
}

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Jsonata.Net.Js;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;

namespace BenchmarkApp
{
    [MemoryDiagnoser]
    public class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Program>();
        }

        private readonly string m_data;
        private readonly string m_query;
        private readonly JsonataEngine m_jsEngine;
        private readonly int m_iterations = 1;

        public Program()
        {
            Console.WriteLine(Directory.GetCurrentDirectory());
            this.m_data = File.ReadAllText("employees.json");
            this.m_query = @"
                {
                  'name': Employee.FirstName & ' ' & Employee.Surname,
                  'mobile': Contact.Phone[type = 'mobile'].number
                }
            ";

            this.m_jsEngine = new Jsonata.Net.Js.JsonataEngine();
        }

        [Benchmark]
        public void ProcessNative()
        {
            Jsonata.Net.Native.JsonataQuery query = new Jsonata.Net.Native.JsonataQuery(this.m_query);
            JToken json = JToken.Parse(this.m_data);
            for (int i = 0; i < this.m_iterations; ++i)
            {
                query.Eval(json);
            }
        }

        [Benchmark]
        public void ProcessJs()
        {
            for (int i = 0; i < this.m_iterations; ++i)
            {
                m_jsEngine.Execute(this.m_query, this.m_data);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonataExerciser
{
    internal sealed record Sample(string name, string data, string query, string bindings)
    { 
        public override string ToString()
        {
            return this.name;
        }

        internal static IEnumerable<Sample> GetDefaultSamples()
        {
            return Directory.EnumerateDirectories(Path.Combine(".", "samples"))
                .Select(dir => CreateFromDirectory(dir));
        }

        private static Sample CreateFromDirectory(string dir)
        {
            string name = Path.GetFileName(dir);
            string data = File.ReadAllText(Path.Combine(dir, "data.json"));
            string query = File.ReadAllText(Path.Combine(dir, "query.jsonata"));
            string bindingsFileName = Path.Combine(dir, "bindings.json");
            string bindings;
            if (File.Exists(bindingsFileName))
            {
                bindings = File.ReadAllText(bindingsFileName);
            }
            else
            {
                bindings = "{}";
            }

            return new Sample(name, data, query, bindings);
        }
    }
}

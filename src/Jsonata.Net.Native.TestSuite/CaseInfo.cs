using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.TestSuite
{
    //see https://github.com/jsonata-js/jsonata/blob/master/test/test-suite/TESTSUITE.md
    public sealed class CaseInfo
    {
        public string? description { get; set; }
        public string? expr { get; set; }
        
        [JsonProperty(PropertyName = "expr-file")] 
        public string? expr_file { get; set; }
        
        public JToken? data { get; set; }
        public string? dataset { get; set; }
        public int? timelimit { get; set; }
        public int? depth { get; set; }
        public JObject? bindings { get; set; }

        public JToken? result { get; set; }
        public bool? undefinedResult { get; set; }
        public string? code { get; set; }
        public string? token { get; set; }

        internal string GetDescription()
        {
            return $"expr: '{this.expr}';\n result: {this.result?.ToString(Formatting.None) ?? ((this.undefinedResult.HasValue && this.undefinedResult.Value) ? "undefined" : "error " + this.code)}";
        }

        public override string ToString()
        {
            return this.expr ?? this.expr_file ?? "<some bad data?>";
        }
    }
}

﻿using Newtonsoft.Json.Linq;
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
        public string expr { get; set; } = "";
        public JToken? data { get; set; }
        public string? dataset { get; set; }
        public int? timelimit { get; set; }
        public int? depth { get; set; }
        public Dictionary<string, object>? bindings { get; set; }

        public JToken? result { get; set; }
        public bool? undefinedResult { get; set; }
        public string? code { get; set; }
        public string? token { get; set; }

        public string info = "";

        public override string ToString()
        {
            return this.info;
        }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.JsonParser.TestSuite
{
    //see https://github.com/nst/JSONTestSuite
    public sealed class CaseInfo
    {
        public string displayName { get; set; } = default!;
        public string fileName { get; set; } = default!;
        public string json { get; set; } = default!;
        public bool? expectedResult { get; set; }
    }
}

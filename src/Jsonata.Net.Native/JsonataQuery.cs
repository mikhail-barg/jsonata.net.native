using Jsonata.Net.Native.Eval;
using Jsonata.Net.Native.Parsing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native
{
    public sealed class JsonataQuery
    {
        private readonly Node m_node;

        public JsonataQuery(string queryText)
        {
            this.m_node = Parser.Parse(queryText);
        }

        public string Eval(string dataJson)
        {
            JToken data = JToken.Parse(dataJson);
            JToken result = this.Eval(data);
            return result.ToString(formatting: Newtonsoft.Json.Formatting.Indented);
        }

        public JToken Eval(JToken data, JObject? bindings = null)
        {
            return EvalProcessor.EvaluateJson(this.m_node, data, bindings);
        }
    }
}

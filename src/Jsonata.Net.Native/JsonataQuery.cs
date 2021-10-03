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

        //TODO:
        //private readonly Dictionary<string, object> m_registry = new Dictionary<string, object>();

        public JsonataQuery(string queryText)
        {
            this.m_node = Parser.Parse(queryText);

            /*
            globalRegistryMutex.RLock()
            this.updateRegistry(globalRegistry)
            globalRegistryMutex.RUnlock()
            */
        }

        public string Eval(string dataJson)
        {
            JToken data = JToken.Parse(dataJson);
            JToken result = this.Eval(data);
            return result.ToString(formatting: Newtonsoft.Json.Formatting.Indented);
        }

        public JToken Eval(JToken data)
        {
            return EvalProcessor.EvaluateJson(this.m_node, data);
        }
    }
}

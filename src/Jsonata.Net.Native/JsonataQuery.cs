using Jsonata.Net.Native.Eval;
using Jsonata.Net.Native.Parsing;
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
            Newtonsoft.Json.Linq.JToken data = Newtonsoft.Json.Linq.JToken.Parse(dataJson);
            Jsonata.Net.Native.Json.JToken result = this.Eval(Jsonata.Net.Native.Json.JToken.FromNewtonsoft(data));
            return result.ToIndentedString();
        }

        public Newtonsoft.Json.Linq.JToken Eval(Newtonsoft.Json.Linq.JToken data, Newtonsoft.Json.Linq.JObject? bindings = null)
        {
            EvaluationEnvironment env;
            if (bindings != null)
            {
                env = new EvaluationEnvironment(bindings);
            }
            else
            {
                env = EvaluationEnvironment.DefaultEnvironment;
            };
            Jsonata.Net.Native.Json.JToken result = EvalProcessor.EvaluateJson(this.m_node, Jsonata.Net.Native.Json.JToken.FromNewtonsoft(data), env);
            return result.ToNewtonsoft();
        }

        internal Jsonata.Net.Native.Json.JToken Eval(Jsonata.Net.Native.Json.JToken data, Jsonata.Net.Native.Json.JObject? bindings = null)
        {
            EvaluationEnvironment env;
            if (bindings != null)
            {
                env = new EvaluationEnvironment(bindings);
            }
            else
            {
                env = EvaluationEnvironment.DefaultEnvironment;
            };
            return EvalProcessor.EvaluateJson(this.m_node, data, env);
        }

        public Newtonsoft.Json.Linq.JToken Eval(Newtonsoft.Json.Linq.JToken data, EvaluationEnvironment environment)
        {
            Jsonata.Net.Native.Json.JToken result = EvalProcessor.EvaluateJson(this.m_node, Jsonata.Net.Native.Json.JToken.FromNewtonsoft(data), environment);
            return result.ToNewtonsoft();
        }

        internal Jsonata.Net.Native.Json.JToken Eval(Jsonata.Net.Native.Json.JToken data, EvaluationEnvironment environment)
        {
            return EvalProcessor.EvaluateJson(this.m_node, data, environment);
        }
    }
}

using System;
using Jsonata.Net.Native.Json;
using Jsonata.Net.Native.Impl;

namespace Jsonata.Net.Native
{
    public sealed class JsonataQuery
    {
        private readonly Node m_ast;

        public static JsonataQuery FromAst(Node ast)
        {
            Node optimized = Optimizer.OptimizeAst(ast);
            return new JsonataQuery(optimized);
        }

        public JsonataQuery(string queryText)
            : this(Parser.Parse(queryText))
        {
        }

        private JsonataQuery(Node ast)
        {
            this.m_ast = ast;
        }

        public JToken Eval(JToken data, EvaluationEnvironment environment)
        {
            return JsonataQ.evaluateMain(this.m_ast, data, environment);
        }

        public override string ToString()
        {
            return this.m_ast.ToString();
        }

        public Node GetAst() 
        { 
            return this.m_ast; 
        }
    }

    public static class JsonataExtensions
    {
        public static JToken Eval(this JsonataQuery query, JToken input)
        {
            return query.Eval(input, bindings: null);
        }

        public static JToken Eval(this JsonataQuery query, JToken input, JObject? bindings)
        {
            EvaluationEnvironment env;
            if (bindings != null)
            {
                env = new EvaluationEnvironment(bindings);
            }
            else
            {
                env = EvaluationEnvironment.DefaultEnvironment;
            }
            return query.Eval(input, env);
        }

        public static string Eval(this JsonataQuery query, string dataJson, bool indentResult = true)
        {
            JToken data = JToken.Parse(dataJson, ParseSettings.DefaultSettings);
            JToken result = query.Eval(data);
            return indentResult ? result.ToIndentedString() : result.ToFlatString();
        }

        public static string Eval(this JsonataQuery query, string dataJson)
        {
            JToken data = JToken.Parse(dataJson, ParseSettings.DefaultSettings);
            JToken result = query.Eval(data);
            return result.ToIndentedString();
        }
    }
}

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    public sealed class Environment
    {
        internal static readonly Environment DefaultEnvironment;

        static Environment()
        {
            Environment.DefaultEnvironment = Environment.CreateDefault();
        }


        private readonly Dictionary<string, JToken> m_bindings = new Dictionary<string, JToken>();
        private readonly Environment? m_parent;
        private readonly EvaluationEnvironment? m_evaluationEnvironment;

        private Environment(Environment? parent, EvaluationEnvironment? evaluationEnvironment)
        {
            this.m_parent = parent;
            this.m_evaluationEnvironment = evaluationEnvironment;
        }

        internal static Environment CreateDefault()
        {
            Environment result = new Environment(null, null);
            foreach (MethodInfo mi in typeof(BuiltinFunctions).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                result.BindFunction(mi);
            }
            return result;
        }

        internal static Environment CreateEvalEnvironment()
        {
            Environment result = new Environment(Environment.DefaultEnvironment, new EvaluationEnvironment());
            return result;
        }

        internal void Bind(string name, JToken value)
        {
            this.m_bindings.Add(name, value);
        }

        internal void BindFunction(MethodInfo mi)
        {
            this.Bind(mi.Name, new FunctionToken(mi.Name, mi));
        }

        internal JToken Lookup(string name)
        {
            if (this.m_bindings.TryGetValue(name, out JToken? result))
            {
                return result;
            }
            else if (this.m_parent != null)
            {
                return this.m_parent.Lookup(name);
            }
            else
            {
                return EvalProcessor.UNDEFINED;
            }
        }

        internal EvaluationEnvironment GetEvaluationEnvironment()
        {
            if (this.m_evaluationEnvironment == null)
            {
                throw new Exception($"Calling {nameof(GetEvaluationEnvironment)}() at non-evaluation env. Should not happen");
            };
            return this.m_evaluationEnvironment;
        }
    }

    //created once for each EvalProcessor.EvaluateJson call
    internal sealed class EvaluationEnvironment
    {
        private readonly Lazy<Random> m_random = new Lazy<Random>();

        internal Random Random => this.m_random.Value;
    }
}

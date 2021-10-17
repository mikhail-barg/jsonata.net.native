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
            Environment.DefaultEnvironment = new Environment(null);
            foreach (MethodInfo mi in typeof(BuiltinFunctions).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                Environment.DefaultEnvironment.Bind(mi.Name, new FunctionToken(mi.Name, mi));
            }
        }

        private readonly Dictionary<string, JToken> m_bindings = new Dictionary<string, JToken>();
        private readonly Environment? m_parent;

        internal Environment(Environment? parent)
        {
            this.m_parent = parent;
        }

        internal void Bind(string name, JToken value)
        {
            this.m_bindings.Add(name, value);
        }

        internal JToken lookup(string name)
        {
            if (this.m_bindings.TryGetValue(name, out JToken? result))
            {
                return result;
            }
            else if (this.m_parent != null)
            {
                return this.m_parent.lookup(name);
            }
            else
            {
                return EvalProcessor.UNDEFINED;
            }
        }
    }
}

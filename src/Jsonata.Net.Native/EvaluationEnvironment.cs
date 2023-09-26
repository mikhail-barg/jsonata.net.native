using Jsonata.Net.Native.Eval;
using Jsonata.Net.Native.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native
{
    public sealed class EvaluationEnvironment
    {
        public static readonly EvaluationEnvironment DefaultEnvironment;

        static EvaluationEnvironment()
        {
            EvaluationEnvironment.DefaultEnvironment = EvaluationEnvironment.CreateDefault();
        }

        internal static EvaluationEnvironment CreateDefault() //main parent, contains default function bindings
        {
            EvaluationEnvironment result = new EvaluationEnvironment(null, null);
            foreach (MethodInfo mi in typeof(BuiltinFunctions).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                result.BindFunction(mi);
            }
            return result;
        }

        //used at actual EvalProcessor.EvaluateJson start to inject EvaluationSupplement
        internal static EvaluationEnvironment CreateEvalEnvironment(EvaluationEnvironment parentEnvironment) 
        {
            EvaluationEnvironment result = new EvaluationEnvironment(parentEnvironment, new EvaluationSupplement());
            return result;
        }

        //used during evaluation when nesting
        internal static EvaluationEnvironment CreateNestedEnvironment(EvaluationEnvironment parent)
        {
            EvaluationEnvironment result = new EvaluationEnvironment(parent, parent.m_evaluationSupplement);
            return result;
        }

        private readonly Dictionary<string, JToken> m_bindings = new Dictionary<string, JToken>();
        private readonly EvaluationEnvironment? m_parent;
        private readonly EvaluationSupplement? m_evaluationSupplement;

        private EvaluationEnvironment(EvaluationEnvironment? parent, EvaluationSupplement? evaluationSupplement)
        {
            this.m_parent = parent;
            this.m_evaluationSupplement = evaluationSupplement;
        }

        //public version to provide for JsonataQuery.Eval()
        public EvaluationEnvironment()
            : this(EvaluationEnvironment.DefaultEnvironment, null)
        {

        }

        public EvaluationEnvironment(JObject bindings)
            : this()
        {
            foreach (KeyValuePair<string, JToken> property in bindings.Properties)
            {
                this.BindValue(property.Key, property.Value);
            }
        }


        public void BindValue(string name, JToken value)
        {
            this.m_bindings[name] = value;  //allow overrides
        }

        public void BindFunction(MethodInfo mi)
        {
            this.BindFunction(mi.Name, mi);
        }

        public void BindFunction(string name, MethodInfo mi)
        {
            this.m_bindings.Add(name, new FunctionTokenCsharp(name, mi));
        }

        public void BindFunction(string name, Delegate funcDelegate)
        {
            this.m_bindings.Add(name, new FunctionTokenCsharp(name, funcDelegate));
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

        internal EvaluationSupplement GetEvaluationSupplement()
        {
            if (this.m_evaluationSupplement == null)
            {
                throw new Exception($"Calling {nameof(GetEvaluationSupplement)}() at non-evaluation env. Should not happen");
            };
            return this.m_evaluationSupplement;
        }
    }
}

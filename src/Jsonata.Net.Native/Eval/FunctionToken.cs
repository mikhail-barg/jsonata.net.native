using Jsonata.Net.Native.Parsing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    internal abstract class FunctionToken: JConstructor
    {
        internal const JTokenType TYPE = JTokenType.Constructor;

        protected FunctionToken(string jsonName)
            :base(jsonName)
        {
        }

        internal abstract int GetArgumentsCount();
    }

    internal sealed class FunctionTokenCsharp: FunctionToken
    {
        internal readonly MethodInfo methodInfo;
        internal readonly string functionName;

        internal FunctionTokenCsharp(string funcName, MethodInfo methodInfo)
            : base($"{methodInfo.DeclaringType?.Name}.{methodInfo.Name}")
        {
            this.functionName = funcName;
            this.methodInfo = methodInfo;
        }

        internal override int GetArgumentsCount()
        {
            //todo: cache in ctor
            return this.methodInfo.GetParameters().Length;
        }
    }

    internal sealed class FunctionTokenLambda : FunctionToken
    {
        internal readonly LambdaNode.Signature? signature;
        internal readonly List<string> paramNames;
        internal readonly Node body;
        internal readonly JToken context;
        internal readonly Environment environment;


        internal FunctionTokenLambda(LambdaNode.Signature? signature, List<string> paramNames, Node body, JToken context, Environment environment)
            : base("lambda")
        {
            this.signature = signature;
            this.paramNames = paramNames;
            this.body = body;
            this.context = context;
            this.environment = environment;
        }

        internal override int GetArgumentsCount()
        {
            return this.paramNames.Count;
        }
    }

    internal sealed class FunctionTokenPartial : FunctionToken
    {
        internal readonly FunctionToken func;
        internal readonly List<JToken?> argsOrPlaceholders;

        internal FunctionTokenPartial(FunctionToken func, List<JToken?> argsOrPlaceholders)
            : base(func.Name + "_partial")
        {
            this.func = func;
            this.argsOrPlaceholders = argsOrPlaceholders;
        }

        internal override int GetArgumentsCount()
        {
            //todo: cache in ctor
            return this.argsOrPlaceholders.Count(t => t == null);
        }
    }
}

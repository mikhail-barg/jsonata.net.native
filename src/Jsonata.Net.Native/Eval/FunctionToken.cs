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
    }
}

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    internal sealed class FunctionToken: JConstructor
    {
        internal const JTokenType TYPE = JTokenType.Constructor;

        internal readonly string functionName;
        internal readonly MethodInfo methodInfo;

        internal FunctionToken(string funcName, MethodInfo methodInfo)
            :base($"{methodInfo.DeclaringType?.Name}.{methodInfo.Name}")
        {
            this.functionName = funcName;
            this.methodInfo = methodInfo;
        }
    }
}

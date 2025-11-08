using System;
using System.Collections.Generic;
using Jsonata.Net.Native.Json;
using Jsonata.Net.Native.New;

namespace Jsonata.Net.Native.Eval
{
    //see jsonata.js evaluateFunction
    internal sealed class FunctionTokenClosure: FunctionToken
    {
        internal readonly FunctionToken proc;
        internal readonly EvaluationEnvironment environment;

        internal FunctionTokenClosure(FunctionToken proc, EvaluationEnvironment environment)
            :base(jsonName: "closure", argumentsCount:proc.ArgumentsCount)  //closure.arity = getFunctionArity(arg);
        {
            this.proc = proc;
            this.environment = environment;
            this.RequiredArgsCount = proc.RequiredArgsCount; //closure.arity = getFunctionArity(arg);
        }

        internal override JToken Apply(JToken? focus_input, EvaluationEnvironment? focus_environment, List<JToken> args)
        {
            return JsonataQ.apply(this.proc, args, JValue.CreateNull(), this.environment);
        }

        public override JToken DeepClone()
        {
            return new FunctionTokenClosure(this.proc, this.environment);
        }
    }
}

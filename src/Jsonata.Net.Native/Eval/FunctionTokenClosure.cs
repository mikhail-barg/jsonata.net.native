using System;
using System.Collections.Generic;
using Jsonata.Net.Native.Json;
using Jsonata.Net.Native.Impl;

namespace Jsonata.Net.Native.Eval
{
    //see jsonata.js evaluateFunction
    internal sealed class FunctionTokenClosure: FunctionToken
    {
        internal readonly FunctionToken proc;
        internal readonly EvaluationEnvironment environment;

        internal FunctionTokenClosure(FunctionToken proc, EvaluationEnvironment environment)
            :base(jsonName: "closure", argumentsCount:proc.ArgumentsCount, signature: null)  //closure.arity = getFunctionArity(arg);
        {
            this.proc = proc;
            this.environment = environment;
            this.RequiredArgsCount = proc.RequiredArgsCount; //closure.arity = getFunctionArity(arg);
        }

        internal override JToken Apply(JsThisArgument jsThis, List<JToken> args)
        {
            //as we see in js code `this` is not used here, instead the captured args are passed to the apply() call
            // 
            // wrap this in a closure
            // const closure = async function(...params) {
            //     // invoke func
            //     return await apply(arg, params, null, environment);
            // };
            return JsonataQ.apply(this.proc, args, JValue.CreateNull(), this.environment);
        }

        public override JToken DeepClone()
        {
            return new FunctionTokenClosure(this.proc, this.environment);
        }
    }
}

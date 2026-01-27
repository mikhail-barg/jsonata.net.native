using System;
using System.Collections.Generic;
using System.Text;
using Jsonata.Net.Native.Json;
using Jsonata.Net.Native.New;

namespace Jsonata.Net.Native.Eval
{

    //These are the ones, for which _jsonata_lambda is true
    internal sealed class FunctionTokenLambda: FunctionToken
    {
        internal readonly JToken input;
        internal readonly EvaluationEnvironment environment;
        internal readonly List<Node> arguments;
        internal readonly Node body;
        internal readonly bool thunk;

        //see jsonata.js evaluateLambda 
        internal FunctionTokenLambda(LambdaNode expr, JToken input, EvaluationEnvironment environment)
            :this(input: input, environment: environment, arguments: expr.arguments, signature: expr.signature, body: expr.body!, thunk: expr.thunk)
        {
        }

        //see jsonata.js partialApplyProcedure 
        internal FunctionTokenLambda(JToken input, EvaluationEnvironment environment, List<Node> arguments, Node body)
            : this(input: input, environment: environment, arguments: arguments, signature: null, body: body, thunk: false)
        {
        }

        private FunctionTokenLambda(JToken input, EvaluationEnvironment environment, List<Node> arguments, Signature? signature, Node body, bool thunk)
            : base(jsonName: "lambda", argumentsCount: arguments.Count, signature: signature)
        {
            this.input = input;
            this.environment = environment;
            this.arguments = arguments;
            this.body = body;
            this.thunk = thunk;
        }

        internal override JToken Apply(JsThisArgument jsThis, List<JToken> args)
        {
            // it's not clear how the `self` here could be null
            //
            // procedure.apply = async function(self, args) {
            //     return await apply(procedure, args, input, !!self ? self.environment : environment);
            // };
            return JsonataQ.apply(this, args, this.input, jsThis.Environment);
        }

        public override JToken DeepClone()
        {
            return new FunctionTokenLambda(input: this.input, environment: this.environment, arguments: this.arguments, signature: this.Signature, body: this.body, thunk: this.thunk);
        }
    }
}

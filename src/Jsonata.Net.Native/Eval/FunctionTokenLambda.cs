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
        internal readonly List<Symbol> arguments;
        internal readonly Signature? signature;
        internal readonly Symbol body;
        internal readonly bool thunk;

        //see jsonata.js evaluateLambda 
        internal FunctionTokenLambda(Symbol expr, JToken input, EvaluationEnvironment environment)
            :this(input: input, environment: environment, arguments: expr.arguments!, signature: expr.signature, body: expr.body!, thunk: expr.thunk)
        {
        }

        //see jsonata.js partialApplyProcedure 
        internal FunctionTokenLambda(JToken input, EvaluationEnvironment environment, List<Symbol> arguments, Symbol body)
            : this(input: input, environment: environment, arguments: arguments, signature: null, body: body, thunk: false)
        {
        }

        private FunctionTokenLambda(JToken input, EvaluationEnvironment environment, List<Symbol> arguments, Signature? signature, Symbol body, bool thunk)
            : base(jsonName: "lambda", argumentsCount: arguments.Count)
        {
            this.input = input;
            this.environment = environment;
            this.arguments = arguments;
            this.signature = signature;
            this.body = body;
            this.thunk = thunk;
        }

        internal override JToken Apply(JToken? focus_input, EvaluationEnvironment? focus_environment, List<JToken> args)
        {
            return JsonataQ.apply(this, args, this.input, focus_environment ?? this.environment);
        }

        public override JToken DeepClone()
        {
            return new FunctionTokenLambda(input: this.input, environment: this.environment, arguments: this.arguments, signature: this.signature, body: this.body, thunk: this.thunk);
        }
    }
}

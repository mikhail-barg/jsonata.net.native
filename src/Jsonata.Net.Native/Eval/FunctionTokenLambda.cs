using Jsonata.Net.Native.Json;
using Jsonata.Net.Native.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    internal sealed class FunctionTokenLambda : FunctionToken
    {
        internal readonly LambdaNode.Signature? signature;
        internal readonly List<string> paramNames;
        internal readonly Node body;
        internal readonly JToken context;
        internal readonly EvaluationEnvironment environment;


        internal FunctionTokenLambda(LambdaNode.Signature? signature, List<string> paramNames, Node body, JToken context, EvaluationEnvironment environment)
            : base("lambda", paramNames.Count)
        {
            this.signature = signature;
            this.paramNames = paramNames;
            this.body = body;
            this.context = context;
            this.environment = environment;
        }

        internal override JToken Invoke(List<JToken> args, JToken? context, EvaluationEnvironment env)
        {
            if (this.signature != null)
            {
                args = this.ValidateSignature(args, context);
            };
            List<(string, JToken)> alignedArgs = this.AlignArgs(args);

            EvaluationEnvironment executionEnv = EvaluationEnvironment.CreateNestedEnvironment(this.environment);
            foreach ((string name, JToken value) in alignedArgs)
            {
                executionEnv.BindValue(name, value);
            };

            JToken result = EvalProcessor.Eval(this.body, this.context, executionEnv);
            return result;
        }

        private List<(string, JToken)> AlignArgs(List<JToken> args)
        {
            List<(string, JToken)> result = new List<(string, JToken)>(this.paramNames.Count);
            //for some reson jsonata does not care if function invocation args does not match expected number of args 
            // - in case when there's no signature specified
            // see for example lambdas.case010 test

            for (int i = 0; i < this.paramNames.Count; ++i)
            {
                JToken value;
                if (i >= args.Count)
                {
                    value = EvalProcessor.UNDEFINED;
                }
                else
                {
                    value = args[i];
                };
                result.Add((this.paramNames[i], value));
            }
            return result;
        }

        private List<JToken> ValidateSignature(List<JToken> args, JToken? inputAsContext)
        {
            throw new NotImplementedException();
        }

        public override JToken DeepClone()
        {
            return new FunctionTokenLambda(this.signature, this.paramNames, this.body, context.DeepClone(), this.environment);
        }

        protected override void CleaParentNested()
        {
            //not sure if context parent should be cleared or not
            this.context.ClearParent();
        }
    }
}

using Jsonata.Net.Native.Parsing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    internal static class EvalProcessor_Lambda
	{
		internal static JToken CallLambdaFunction(FunctionTokenLambda lambdaFunction, List<JToken> args, JToken? inputAsContext)
		{
			if (lambdaFunction.signature != null)
            {
				args = ValidateSignature(lambdaFunction.signature, args, inputAsContext);
            };
			List<(string, JToken)> alignedArgs = AlignArgs(lambdaFunction.paramNames, args);

			Environment executionEnv = Environment.CreateNestedEnvironment(lambdaFunction.environment);
			foreach ((string name, JToken value) in alignedArgs)
            {
				executionEnv.Bind(name, value);
            };

			JToken result = EvalProcessor.Eval(lambdaFunction.body, lambdaFunction.context, executionEnv);
			return result;
		}

        private static List<(string, JToken)> AlignArgs(List<string> paramNames, List<JToken> args)
        {
			List<(string, JToken)> result = new List<(string, JToken)>(paramNames.Count);
			//for some reson jsonata does not care if function invocation args does not match expected number of args 
			// - in case when there's no signature specified
			// see for example lambdas.case010 test

			for (int i = 0; i < paramNames.Count; ++i)
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
				result.Add((paramNames[i], value));
            }
			return result;
        }

        private static List<JToken> ValidateSignature(LambdaNode.Signature signature, List<JToken> args, JToken? inputAsContext)
        {
            throw new NotImplementedException();
        }
	}
}

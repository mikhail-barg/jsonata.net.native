using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Jsonata.Net.Native.New;

namespace Jsonata.Net.Native.Eval
{
    internal sealed class FunctionTokenPartial : FunctionToken
    {
        internal readonly FunctionToken func;
        internal readonly List<JToken?> argsOrPlaceholders;

        internal FunctionTokenPartial(FunctionToken func, List<JToken?> argsOrPlaceholders)
            : base(func.Name + "_partial", argsOrPlaceholders.Count(t => t == null))
        {
            this.func = func;
            this.argsOrPlaceholders = argsOrPlaceholders;
        }

        internal override JToken Apply(JToken? focus_input, EvaluationEnvironment? focus_environment, List<JToken> args)
        {
            List<JToken> alignedArgs = this.AlignPartialFunctionArgs(args, focus_input);
            JToken result = this.func.Apply(focus_input, focus_environment, alignedArgs);
            return result;
        }

        private List<JToken> AlignPartialFunctionArgs(List<JToken> args, JToken? context)
        {
            int expectedArgsCount = this.ArgumentsCount;
            bool useContext = false;
            if (expectedArgsCount == args.Count + 1 && context != null)
            {
                useContext = true;
            };
            int nextArgIndex = 0;
            List<JToken> result = new List<JToken>(this.argsOrPlaceholders.Count);
            foreach (JToken? arg in this.argsOrPlaceholders)
            {
                if (arg == null)
                {
                    if (useContext)
                    {
                        result.Add(context!);
                        useContext = false;
                    }
                    else if (nextArgIndex < args.Count)
                    {
                        result.Add(args[nextArgIndex]);
                        ++nextArgIndex;
                    }
                    else
                    {
                        result.Add(JsonataQ.UNDEFINED);
                    }
                }
                else
                {
                    result.Add(arg);
                }
            };
            return result;
        }

        public override JToken DeepClone()
        {
            return new FunctionTokenPartial(this.func, this.argsOrPlaceholders.Select(i => i?.DeepClone()).ToList());
        }

    }
}

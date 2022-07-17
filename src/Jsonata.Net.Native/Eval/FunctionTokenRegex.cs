using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;

namespace Jsonata.Net.Native.Eval
{
    internal sealed class FunctionTokenRegex : FunctionToken
    {
        internal readonly Regex regex;

        public FunctionTokenRegex(Regex regex)
            : base("regex", 1)
        {
            this.regex = regex;
        }

        /**
            The ~> is the chain operator, and its use here implies that the result of /regex/ is a function. 
            We'll see below that this is in fact the case.         
         */
        internal override JToken Invoke(List<JToken> args, JToken? context, EvaluationEnvironment env)
        {
            JToken arg;
            if (args.Count == 0 && context != null)
            {
                arg = context;
            }
            else if (args.Count > 0)
            {
                arg = args[0];
            }
            else
            {
                arg = EvalProcessor.UNDEFINED;
            }

            switch (arg.Type)
            {
            case JTokenType.Undefined:
                return EvalProcessor.UNDEFINED;
            case JTokenType.String:
                break;
            default:
                throw new JsonataException("????", $"Argument 1 of regex should be String, got {arg.Type}");
            }

            string str = (string)arg!;
            Match match = this.regex.Match(str);
            if (!match.Success)
            {
                return EvalProcessor.UNDEFINED;
            }
            return ConvertRegexMatch(match);
        }

        internal static JObject ConvertRegexMatch(Match match)
        {
            JObject result = new JObject();
            result.Add("match", new JValue(match.Value));
            result.Add("index", new JValue(match.Index));
            if (match.Groups.Count > 1) //0th is a whole regex
            {
                JArray groups = new JArray();
                for (int i = 1; i < match.Groups.Count; ++i)
                {
                    groups.Add(new JValue(match.Groups[i].Value));
                };
                result.Add("groups", groups);
            }
            //TODO: add "next", see http://docs.jsonata.org/regex#generic-matchers
            return result;
        }

        internal override JToken DeepClone()
        {
            return new FunctionTokenRegex(this.regex);
        }
    }
}

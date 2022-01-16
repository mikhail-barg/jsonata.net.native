using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    internal static class EvalProcessor_Regex
    {
		internal static JToken CallRegexFunction(FunctionTokenRegex regexFunction, List<JToken> args, JToken? inputAsContext)
		{
			JToken arg;
			if (args.Count == 0 && inputAsContext != null)
            {
				arg = inputAsContext;
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
            Match match = regexFunction.regex.Match(str);
            if (!match.Success)
            {
                return EvalProcessor.UNDEFINED;
            }
            return EvalProcessor_Regex.ConvertRegexMatch(match);
		}

        internal static JObject ConvertRegexMatch(Match match)
        {
            JObject result = new JObject();
            result.Add("match", match.Value);
            result.Add("index", match.Index);
            if (match.Groups.Count > 1) //0th is a whole regex
            {
                JArray groups = new JArray();
                for (int i = 1; i < match.Groups.Count; ++i)
                {
                    groups.Add(match.Groups[i].Value);
                };
                result.Add("groups", groups);
            }
            //TODO: add "next", see http://docs.jsonata.org/regex#generic-matchers
            return result;
        }
    }
}

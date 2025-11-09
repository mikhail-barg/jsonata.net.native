using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Jsonata.Net.Native.New;

namespace Jsonata.Net.Native.Eval
{
    // see jsonata.js evaluateRegex
    // this one is "closure"
    internal sealed class FunctionTokenRegex : FunctionToken
    {
        internal readonly Regex regex;

        public FunctionTokenRegex(Regex regex)
            : base("regex", 1, signature: null)
        {
            this.regex = regex;
        }

        internal static JToken InvokeClosure(Regex regex, string str, int fromIndex)
        {
            Match match = regex.Match(str, fromIndex);
            if (!match.Success)
            {
                return JsonataQ.UNDEFINED;
            }

            JObject result = new JObject();
            result.Add("match", new JValue(match.Value));
            result.Add("start", new JValue(match.Index));
            result.Add("end", new JValue(match.Index + match.Value.Length));
            JArray groups = new JArray(match.Groups.Count - 1);
            if (match.Groups.Count > 1) //0th is a whole regex
            {
                for (int i = 1; i < match.Groups.Count; ++i)
                {
                    groups.Add(new JValue(match.Groups[i].Value));
                }
            }
            result.Add("groups", groups);
            result.Add("next", new FunctionTokenNextMatch(regex, str, fromIndex: match.Index + match.Value.Length));

            return result;
        }

        internal override JToken Apply(JToken? focus_input, EvaluationEnvironment? focus_environment, List<JToken> args)
        {
            JToken arg;
            if (args.Count == 0 && focus_input != null)
            {
                arg = focus_input;
            }
            else if (args.Count > 0)
            {
                arg = args[0];
            }
            else
            {
                arg = JsonataQ.UNDEFINED;
            }

            switch (arg.Type)
            {
            case JTokenType.Undefined:
                return JsonataQ.UNDEFINED;
            case JTokenType.String:
                break;
            default:
                throw new JsonataException("????", $"Argument 1 of regex should be String, got {arg.Type}");
            }

            string str = (string)arg!;
            return InvokeClosure(this.regex, str, 0);
        }

        public override JToken DeepClone()
        {
            return new FunctionTokenRegex(this.regex);
        }


        // this one is "next = function()"
        private sealed class FunctionTokenNextMatch : FunctionToken
        {
            private readonly Regex m_regex;
            private readonly string m_str;
            private readonly int m_fromIndex;

            public FunctionTokenNextMatch(Regex regex, string str, int fromIndex) 
                :base("_regex_match", 0, signature: null)
            {
                this.m_regex = regex;
                this.m_str = str;
                this.m_fromIndex = fromIndex;
            }

            internal override JToken Apply(JToken? focus_input, EvaluationEnvironment? focus_environment, List<JToken> args)
            {
                if (this.m_fromIndex >= this.m_str.Length)
                {
                    return JsonataQ.UNDEFINED;
                }

                JToken next = FunctionTokenRegex.InvokeClosure(this.m_regex, this.m_str, this.m_fromIndex);
                if (next.Type == JTokenType.Undefined)
                {
                    return next;
                }
                JObject nextMatch = (JObject)next;
                if ((string)nextMatch.Properties["match"] == "")
                {
                    throw new JException("D1004");
                }
                return next;
            }

            public override JToken DeepClone()
            {
                return new FunctionTokenNextMatch(this.m_regex, this.m_str, this.m_fromIndex);
            }
        }
    }
}

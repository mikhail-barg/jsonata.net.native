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
    internal sealed class FunctionTokenRegex : FunctionToken
    {
        internal readonly Regex regex;

        public FunctionTokenRegex(Regex regex)
            : base("regex", 1)
        {
            this.regex = regex;
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
            FunctionTokenNextMatch closure = new FunctionTokenNextMatch(this.regex, str, 0);
            JToken result = closure.Apply(null, null, new List<JToken>());
            return result;
        }

        public override JToken DeepClone()
        {
            return new FunctionTokenRegex(this.regex);
        }


        // var closure = function(str, fromIndex)
        private sealed class FunctionTokenNextMatch : FunctionToken
        {
            private readonly Regex m_regex;
            private readonly string m_str;
            private readonly int m_fromIndex;

            public FunctionTokenNextMatch(Regex regex, string str, int fromIndex) 
                :base("_regex_match", 0)
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

                Match match = this.m_regex.Match(this.m_str, this.m_fromIndex);
                if (match == null)
                {
                    return JsonataQ.UNDEFINED;
                }

                JObject result = new JObject();
                result.Add("match", new JValue(match.Value));
                result.Add("index", new JValue(match.Index));
                JArray groups = new JArray(match.Groups.Count - 1);
                if (match.Groups.Count > 1) //0th is a whole regex
                {
                    for (int i = 1; i < match.Groups.Count; ++i)
                    {
                        groups.Add(new JValue(match.Groups[i].Value));
                    }
                }
                result.Add("groups", groups);
                result.Add("next", new FunctionTokenNextMatch(this.m_regex, this.m_str, fromIndex: match.Index + match.Value.Length));

                return result;
            }

            public override JToken DeepClone()
            {
                return new FunctionTokenNextMatch(this.m_regex, this.m_str, this.m_fromIndex);
            }
        }
    }
}

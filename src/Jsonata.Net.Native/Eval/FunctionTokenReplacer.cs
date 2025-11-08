using System;
using System.Collections.Generic;
using System.Text;
using Jsonata.Net.Native.Json;

namespace Jsonata.Net.Native.Eval
{
    //see functions.js replace
    internal sealed class FunctionTokenReplacer : FunctionToken
    {
        private readonly string replacement;

        internal FunctionTokenReplacer(string replacement)
            :base("_replacer", 1)
        {
            this.replacement = replacement;
        }

        internal override JToken Apply(JToken? focus_input, EvaluationEnvironment? focus_environment, List<JToken> args)
        {
            JObject regexMatch = (JObject)args[0];
            StringBuilder substitute = new StringBuilder();
            // scan forward, copying the replacement text into the substitute string
            // and replace any occurrence of $n with the values matched by the regex
            int position = 0;
            int index = this.replacement.IndexOf('$', position);
            while (index >= 0 && position < this.replacement.Length)
            {
                substitute.Append(this.replacement.Substring(position, index - position));
                position = index + 1;
                // var dollarVal = replacement.charAt(position); -- returns empty string for out of bounds
                char? dollarVal = position < 0 || position >= this.replacement.Length? null : this.replacement[position];
                if (dollarVal == '$')
                {
                    // literal $
                    substitute.Append('$');
                    ++position;
                }
                else if (dollarVal == '0')
                {
                    substitute.Append((string)(JValue)regexMatch.Properties["match"]);
                    ++position;
                }
                else
                {
                    int maxDigits;
                    JArray groups = (JArray)regexMatch.Properties["groups"];
                    if (groups.Count == 0)
                    {
                        // no sub-matches; any $ followed by a digit will be replaced by an empty string
                        maxDigits = 1;
                    }
                    else
                    {
                        // max number of digits to parse following the $
                        maxDigits = (int)(Math.Log10(groups.Count)) + 1;
                    }
                    bool isNotNan = parseInt(replacement, position, maxDigits, out index, out int parsedChars);
                    if (maxDigits > 1 && index > groups.Count)
                    {
                        isNotNan = parseInt(replacement, position, maxDigits - 1, out index, out parsedChars);
                    }
                    if (isNotNan)
                    {
                        if (groups.Count > 0)
                        {
                            //var submatch = regexMatch.groups[index - 1]; -- here js will return undefined for out of bounds!!
                            //if (typeof submatch !== 'undefined')
                            int groupIndex = index - 1;
                            if (groupIndex >= 0 && groupIndex < groups.Count)
                            {
                                JToken submatch = groups.ChildrenTokens[index - 1];
                                if (submatch.Type != JTokenType.Undefined)
                                {
                                    substitute.Append((string)(JValue)submatch);
                                }
                            }
                        }
                        position += parsedChars;
                    }
                    else
                    {
                        // not a capture group, treat the $ as literal
                        substitute.Append('$');
                    }
                }
                index = replacement.IndexOf('$', position);
            }
            substitute.Append(replacement.Substring(position));
            return new JValue(substitute.ToString());
        }

        private static bool parseInt(string str, int startPos, int length, out int result, out int consumedChars)
        {
            result = 0;
            consumedChars = 0;
            if (str.Length == 0 || startPos >= str.Length ||  str[startPos] < '0' || str[startPos] > '9')
            {
                return false;
            }

            for (int i = startPos; i < (startPos + length) && i < str.Length; ++i)
            {
                char c = str[i];
                if (c >= '0' && c <= '9')
                {
                    result = result * 10 + (c - '0');
                    ++consumedChars;
                }
                else
                {
                    break;
                }
            }
            return true;
        }

        public override JToken DeepClone()
        {
            throw new NotImplementedException();
        }
    }
}

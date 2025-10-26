using Jsonata.Net.Native.Json;
using Jsonata.Net.Native.New;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    public static class BuiltinFunctions
    {
        private static readonly Encoding UTF8_NO_BOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        #region String functions
        /**
        Signature: $string(arg, prettify)

        Casts the arg parameter to a string using the following casting rules

        If arg is not specified (i.e. this function is invoked with no arguments), then the context value is used as the value of arg.

        If prettify is true, then "prettified" JSON is produced. i.e One line per field and lines will be indented based on the field depth.
        */
        public static JToken @string([AllowContextAsValue] JToken arg, [OptionalArgument(false)] bool prettify)
        {
            switch (arg.Type)
            {
            case JTokenType.Undefined:
                // undefined inputs always return undefined
                return arg;
            case JTokenType.String:
                //Strings are unchanged
                return arg;
            case JTokenType.Float:
                {
                    double value = (double)arg;
                    if (Double.IsNaN(value) || Double.IsInfinity(value))
                    {
                        throw new JsonataException("D3001", "Attempting to invoke string function on Infinity or NaN");
                    };
                    return new JValue(arg.ToFlatString());
                };
            case JTokenType.Function:
                //Functions are converted to an empty string
                return new JValue("");
            default:
                return new JValue(prettify? arg.ToIndentedString() : arg.ToFlatString());
            }
        }

        /**
         Signature: $length(str)
         Returns the number of characters in the string str. 
         If str is not specified (i.e. this function is invoked with no arguments), then the context value is used as the value of str. 
         An error is thrown if str is not a string.
         */
        public static int length([AllowContextAsValue][PropagateUndefined] string str)
        {
            return str.Length;
        }

        /**
        Signature: $substring(str, start[, length])
        Returns a string containing the characters in the first parameter str starting at position start (zero-offset). 
        If str is not specified (i.e. this function is invoked with only the numeric argument(s)), then the context value is used as the value of str. An error is thrown if str is not a string.
        If length is specified, then the substring will contain maximum length characters.
        If start is negative then it indicates the number of characters from the end of str. See substr for full definition.         
         */
        public static string substring([AllowContextAsValue][PropagateUndefined] string str, int start, [OptionalArgument(100000000)] int len)
        {
            //see https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/substr
            if (start < 0)
            {
                start = str.Length + start;
                if (start < 0)
                {
                    start = 0;
                }
            };
            if (start + len > str.Length)
            {
                len = str.Length - start;
            };
            if (len < 0)
            {
                len = 0;
            };
            return str.Substring(start, len);
        }

        /**
         Signature: $substringBefore(str, chars)
         Returns the substring before the first occurrence of the character sequence chars in str. 
         If str is not specified (i.e. this function is invoked with only one argument), then the context value is used as the value of str. 
         If str does not contain chars, then it returns str. An error is thrown if str and chars are not strings.         
         */
        public static string substringBefore([AllowContextAsValue][PropagateUndefined] string str, string chars)
        {
            int index = str.IndexOf(chars);
            if (index < 0)
            {
                return str;
            }
            else
            {
                return str.Substring(0, index);
            }
        }

        /**
          Signature: $substringAfter(str, chars)
          Returns the substring after the first occurrence of the character sequence chars in str. 
          If str is not specified (i.e. this function is invoked with only one argument), then the context value is used as the value of str. 
          If str does not contain chars, then it returns str. An error is thrown if str and chars are not strings.         
         */
        public static string substringAfter([AllowContextAsValue][PropagateUndefined] string str, string chars)
        {
            int index = str.IndexOf(chars);
            if (index < 0)
            {
                return str;
            }
            else
            {
                return str.Substring(index + chars.Length);
            }
        }

        /**
          Signature: $uppercase(str)
          Returns a string with all the characters of str converted to uppercase. 
          If str is not specified (i.e. this function is invoked with no arguments), then the context value is used as the value of str. 
          An error is thrown if str is not a string.         
         */
        public static string uppercase([AllowContextAsValue][PropagateUndefined] string str)
        {
            return str.ToUpper();
        }

        /**
          Signature: $lowercase(str)
          Returns a string with all the characters of str converted to lowercase. 
          If str is not specified (i.e. this function is invoked with no arguments), then the context value is used as the value of str. 
          An error is thrown if str is not a string.         
         */
        public static string lowercase([AllowContextAsValue][PropagateUndefined] string str)
        {
            return str.ToLower();
        }

        /**
         Signature: $trim(str)
         Normalizes and trims all whitespace characters in str by applying the following steps:
            All tabs, carriage returns, and line feeds are replaced with spaces.
            Contiguous sequences of spaces are reduced to a single space.
            Trailing and leading spaces are removed.
          If str is not specified (i.e. this function is invoked with no arguments), then the context value is used as the value of str. 
          An error is thrown if str is not a string.         
         */
        public static string trim([AllowContextAsValue][PropagateUndefined] string str)
        {
            str = Regex.Replace(str, @"\s+", " ");
            return str.Trim();
        }

        /**
         Signature: $pad(str, width [, char])
         Returns a copy of the string str with extra padding, if necessary, so that its total number of characters is at least the absolute value of the width parameter. 
         If width is a positive number, then the string is padded to the right; 
         if negative, it is padded to the left. 
         The optional char argument specifies the padding character(s) to use. 
         If not specified, it defaults to the space character        
         */
        public static string pad([AllowContextAsValue][PropagateUndefined] string str, int width, [OptionalArgument(" ")] string chars)
        {
            if (chars == "")
            {
                chars = " ";
            };

            if (width >= 0)
            {
                bool changed = false;
                while (str.Length < width)
                {
                    str += chars;
                    changed = true;
                };
                if (changed && str.Length > width)
                {
                    str = str.Substring(0, width);
                };
            }
            else
            {
                width = -width;
                bool changed = false;
                while (str.Length < width)
                {
                    str = chars + str;
                    changed = true;
                };
                if (changed && str.Length > width)
                {
                    str = str.Substring(str.Length - width);
                };
            };

            return str;
        }

        /**
         Signature: $contains(str, pattern)
         Returns true if str is matched by pattern, otherwise it returns false. 
         If str is not specified (i.e. this function is invoked with one argument), then the context value is used as the value of str.
         The pattern parameter can either be a string or a regular expression (regex). 
          If it is a string, the function returns true if the characters within pattern are contained contiguously within str. 
          If it is a regex, the function will return true if the regex matches the contents of str.      
        
            Examples

                $contains("abracadabra", "bra") => true
                $contains("abracadabra", /a.*a/) => true
                $contains("abracadabra", /ar.*a/) => false
                $contains("Hello World", /wo/) => false
                $contains("Hello World", /wo/i) => true
         */
        public static bool contains([AllowContextAsValue][PropagateUndefined] string str, JToken pattern)
        {
            switch (pattern.Type)
            {
            case JTokenType.String:
                return str.Contains((string)pattern!);
            case JTokenType.Function:
                if (pattern is not FunctionTokenRegex regex)
                {
                    throw new JsonataException("T0410", $"Argument 2 of function {nameof(contains)} should be either string or regex. Passed function {pattern.GetType().Name})");
                }
                else
                {
                    return regex.regex.IsMatch(str);
                }
            default:
                throw new JsonataException("T0410", $"Argument 2 of function {nameof(contains)} should be either string or regex. Passed {pattern.Type} ({pattern.ToFlatString()})");
            }
        }

        /**
        Signature: $split(str, separator [, limit])

        Splits the str parameter into an array of substrings. 
        If str is not specified, then the context value is used as the value of str. 
        It is an error if str is not a string.

        The separator parameter can either be a string or a regular expression (regex). 
        If it is a string, it specifies the characters within str about which it should be split. 
        If it is the empty string, str will be split into an array of single characters. 
        If it is a regex, it splits the string around any sequence of characters that match the regex.

        The optional limit parameter is a number that specifies the maximum number of substrings to include in the resultant array. 
        Any additional substrings are discarded. 
        If limit is not specified, then str is fully split with no limit to the size of the resultant array. 
        It is an error if limit is not a non-negative number.         
         */
        public static JArray split([PropagateUndefined] string str, JToken separator, [OptionalArgument(Int32.MaxValue)] int limit)
        {
            //TODO: support RegExes!!

            if (limit < 0)
            {
                throw new JsonataException("D3020", $"Third argument of {nameof(split)} function must evaluate to a positive number. Passed {limit}");
            }

            JArray result = new JArray();

            switch (separator.Type)
            {
            case JTokenType.String:
                {
                    string separatorString = (string)separator!;
                    if (separatorString == "")
                    {
                        foreach (char c in str)
                        {
                            if (result.Count >= limit)
                            {
                                break;
                            }
                            result.Add(new JValue(c));
                        }
                    }
                    else
                    {
                        foreach (string part in Regex.Split(str, Regex.Escape(separatorString)))
                        {
                            if (result.Count >= limit)
                            {
                                break;
                            }
                            result.Add(new JValue(part));
                        }
                    }
                }
                break;
            case JTokenType.Function:
                {
                    if (separator is not FunctionTokenRegex regex)
                    {
                        throw new JsonataException("T0410", $"Argument 2 of function {nameof(split)} should be either string or regex. Passed function {separator.GetType().Name})");
                    };
                    foreach (string part in regex.regex.Split(str))
                    {
                        if (result.Count >= limit)
                        {
                            break;
                        }
                        result.Add(new JValue(part));
                    };
                }
                break;
            default:
                throw new JsonataException("T0410", $"Argument 2 of function {nameof(split)} should be either string or regex. Passed {separator.Type} ({separator.ToFlatString()})");
            }
            return result;
        }

        /**
        Signature: $join(array[, separator])
        Joins an array of component strings into a single concatenated string with each component string separated by the optional separator parameter.
        It is an error if the input array contains an item which isn't a string.
        If separator is not specified, then it is assumed to be the empty string, i.e. no separator between the component strings. 
        It is an error if separator is not a string.         
        */
        public static string join([PropagateUndefined] JToken array, [OptionalArgument(null)] JToken? separator)
        {
            string separatorString;
            if (separator == null)
            {
                separatorString = "";
            }
            else
            {
                switch (separator.Type)
                {
                case JTokenType.Undefined:
                    separatorString = "";
                    break;
                case JTokenType.String:
                    separatorString = (string)separator!;
                    break;
                default:
                    throw new JsonataException("T0410", $"Argument 2 of function {nameof(join)} is expected to be string. Specified {separator.Type}");
                }
            };

            List<string> elements = new List<string>();
            switch (array.Type)
            {
            case JTokenType.String:
                elements.Add((string)array!);
                break;
            case JTokenType.Array:
                foreach (JToken element in ((JArray)array).ChildrenTokens)
                {
                    if (element.Type != JTokenType.String)
                    {
                        throw new JsonataException("T0412", $"Argument 1 of function {nameof(join)} must be an array of strings");
                    }
                    else
                    {
                        elements.Add((string)element!);
                    }
                }
                break;
            default:
                throw new JsonataException("T0410", $"Argument 1 of function {nameof(join)} is expected to be an Array. Specified {array.Type}");
            }
            return String.Join(separatorString, elements);
        }


        /**
          Signature: $match(str, pattern [, limit])

          Applies the str string to the pattern regular expression and returns an array of objects, 
            with each object containing information about each occurrence of a match withing str.

          The object contains the following fields:
            match - the substring that was matched by the regex.
            index - the offset (starting at zero) within str of this match.
            groups - if the regex contains capturing groups (parentheses), this contains an array of strings representing each captured group.

        If str is not specified, then the context value is used as the value of str. It is an error if str is not a string.         
        */
        public static JArray match([AllowContextAsValue][PropagateUndefined] string str, JToken pattern, [OptionalArgument(Int32.MaxValue)] int limit)
        {
            if (pattern is not FunctionTokenRegex regex)
            {
                throw new JsonataException("T0410", $"Argument 2 of function {nameof(match)} should be regex. Passed {pattern.Type} ({pattern.ToFlatString()})");
            };

            if (limit < 0)
            {
                throw new JsonataException("D3040", $"Third argument of {nameof(match)} function must evaluate to a positive number");
            };

            JArray result = new JArray();
            foreach (Match match in regex.regex.Matches(str))
            {
                if (result.Count >= limit)
                {
                    break;
                };
                result.Add(FunctionTokenRegex.ConvertRegexMatch(match));
            }

            return result;
        }


        /**
        Signature: $replace(str, pattern, replacement [, limit])
        Finds occurrences of pattern within str and replaces them with replacement.

        If str is not specified, then the context value is used as the value of str. It is an error if str is not a string.

        The pattern parameter can either be a string or a regular expression (regex). 
            If it is a string, it specifies the substring(s) within str which should be replaced. 
            If it is a regex, its is used to find .

        The replacement parameter can either be a string or a function. 
            If it is a string, it specifies the sequence of characters that replace the substring(s) that are matched by pattern. 
            If pattern is a regex, then the replacement string can refer to the characters that were matched by the regex as well as any of the captured groups 
            using a $ followed by a number N:
                If N = 0, then it is replaced by substring matched by the regex as a whole.
                If N > 0, then it is replaced by the substring captured by the Nth parenthesised group in the regex.
                If N is greater than the number of captured groups, then it is replaced by the empty string.
                A literal $ character must be written as $$ in the replacement string

        If the replacement parameter is a function, then it is invoked for each match occurrence of the pattern regex. 
        The replacement function must take a single parameter which will be the object structure of a regex match 
            as described in the $match function; and must return a string.

        The optional limit parameter, is a number that specifies the maximum number of replacements to make before stopping. 
        The remainder of the input beyond this limit will be copied to the output unchanged.         
         */
        public static string replace([PropagateUndefined] string str, JToken pattern, JToken replacement, [OptionalArgument(Int32.MaxValue)] int limit)
        {
            if (limit < 0)
            {
                throw new JsonataException("D3011", $"Fourth argument of {nameof(replace)} function must evaluate to a positive number");
            }
            else if (limit == 0)
            {
                return str;
            }

            switch (pattern.Type)
            {
            case JTokenType.String:
                {
                    string patternString = (string)pattern!;
                    if (patternString == "")
                    {
                        throw new JsonataException("D3010", $"Second argument of {nameof(replace)} function cannot be an empty string");
                    }
                    else
                    {
                        if (replacement.Type != JTokenType.String)
                        {
                            throw new JsonataException("D3012", "Attempted to replace a matched string with a non-string value");
                        };
                        string replacementString = (string)replacement!;
                        StringBuilder builder = new StringBuilder();
                        int replacesCount = 0;
                        int replaceStartAt = 0;
                        while (true)
                        {
                            if (replacesCount >= limit)
                            {
                                break;
                            };
                            int pos = str.IndexOf(patternString, startIndex: replaceStartAt);
                            if (pos < 0)
                            {
                                break;
                            }
                            else
                            {
                                if (pos > replaceStartAt)
                                {
                                    builder.Append(str.Substring(replaceStartAt, pos - replaceStartAt));
                                }
                                builder.Append(replacementString);
                                ++replacesCount;
                                replaceStartAt = pos + patternString.Length;
                            };
                        }
                        if (replaceStartAt < str.Length)
                        {
                            builder.Append(str.Substring(replaceStartAt));
                        };
                        return builder.ToString();
                    }
                }
                //break;
            case JTokenType.Function:
                {
                    if (pattern is not FunctionTokenRegex regex)
                    {
                        throw new JsonataException("T0410", $"Argument 2 of function {nameof(replace)} should be either string or regex. Passed function {pattern.GetType().Name})");
                    };

                    MatchCollection matches = regex.regex.Matches(str);
                    if (matches.Count == 0)
                    {
                        return str;
                    };

                    switch (replacement.Type)
                    {
                    case JTokenType.String:
                        {
                            string replacementString = (string)replacement!;
                            StringBuilder builder = new StringBuilder();
                            int replacesCount = 0;
                            int replaceStartAt = 0;
                            foreach (Match match in matches)
                            {
                                if (replacesCount >= limit)
                                {
                                    break;
                                };
                                if (match.Index < replaceStartAt)
                                {
                                    continue;   //overlapping matches
                                }
                                else if (match.Index > replaceStartAt)
                                {
                                    builder.Append(str.Substring(replaceStartAt, match.Index - replaceStartAt));
                                }
                                //TODO: use ProcessAppendReplacementStringForMatch instead of Result, but actually it's too ugly!
                                //ProcessAppendReplacementStringForMatch(builder, match, replacementString);
                                builder.Append(match.Result(replacementString));
                                ++replacesCount;
                                replaceStartAt = match.Index + match.Length;
                            }
                            if (replaceStartAt < str.Length)
                            {
                                builder.Append(str.Substring(replaceStartAt));
                            };
                            return builder.ToString();
                        }
                        //break;
                    case JTokenType.Function:
                        {
                            throw new NotImplementedException();
                            /*
                            FunctionToken replacementFunction = (FunctionToken)replacement;
                            StringBuilder builder = new StringBuilder();
                            EvaluationEnvironment env = EvaluationEnvironment.CreateEvalEnvironment(EvaluationEnvironment.DefaultEnvironment); //TODO: think of providing proper env. Maybe via a func param?
                            int replacesCount = 0;
                            int replaceStartAt = 0;
                            foreach (Match match in matches)
                            {
                                if (replacesCount >= limit)
                                {
                                    break;
                                };
                                if (match.Index < replaceStartAt)
                                {
                                    continue;   //overlapping matches
                                }
                                else if (match.Index > replaceStartAt)
                                {
                                    builder.Append(str.Substring(replaceStartAt, match.Index - replaceStartAt));
                                };
                                JObject matchObject = FunctionTokenRegex.ConvertRegexMatch(match);
                                JToken replacementToken = EvalProcessor.InvokeFunction(replacementFunction, new List<JToken>() { matchObject }, null, env);
                                if (replacementToken.Type != JTokenType.String)
                                {
                                    throw new JsonataException("D3012", "Attempted to replace a matched string with a non-string value");
                                }
                                builder.Append((string)replacementToken!);
                                ++replacesCount;
                                replaceStartAt = match.Index + match.Length;
                            }
                            if (replaceStartAt < str.Length)
                            {
                                builder.Append(str.Substring(replaceStartAt));
                            };
                            return builder.ToString();
                            */
                        }
                        //break;
                    default:
                        throw new JsonataException("T0410", $"Argument 3 of function {nameof(replace)} should be either string or function. Passed {replacement.Type} ({replacement.ToFlatString()})");
                    };
                }
                //break;
            default:
                throw new JsonataException("T0410", $"Argument 2 of function {nameof(replace)} should be either string or regex. Passed {pattern.Type} ({pattern.ToFlatString()})");
            };

            /*
            //see jsonata-js functions.js around line 440: "replacer = function (regexMatch)"
            void ProcessAppendReplacementStringForMatch(StringBuilder builder, Match match, string replacement)
            {
                int maxDigits;
                if (match.Groups.Count == 1)
                {
                    // no sub-matches; any $ followed by a digit will be replaced by an empty string
                    maxDigits = 1;
                }
                else
                {
                    // max number of digits to parse following the $
                    maxDigits = (int)Math.Floor(Math.Log(match.Groups.Count) * Math.Log10(Math.E)) + 1;
                }


                // scan forward, copying the replacement text into the substitute string
                // and replace any occurrence of $n with the values matched by the regex
                int position = 0;
                int index = replacement.IndexOf('$', position);
                while (index >= 0 && position < replacement.Length)
                {
                    builder.Append(replacement.Substring(position, index - position));
                    position = index + 1;
                    char dollarVal = replacement[position];
                    if (dollarVal == '$')
                    {
                        // literal $
                        builder.Append('$');
                        ++position;
                    }
                    else if (dollarVal == '0')
                    {
                        builder.Append(match.Value);
                        ++position;
                    }
                    else
                    {
                        
                        index = Int32.TryParse(replacement.substring(position, position + maxDigits), 10);
                        if (maxDigits > 1 && index > regexMatch.groups.length)
                        {
                            index = parseInt(replacement.substring(position, position + maxDigits - 1), 10);
                        }
                        if (!isNaN(index))
                        {
                            if (regexMatch.groups.length > 0)
                            {
                                var submatch = regexMatch.groups[index - 1];
                                if (typeof submatch !== 'undefined')
                                {
                                    substitute += submatch;
                                }
                            }
                            position += index.toString().length;
                        }
                        else
                        {
                            // not a capture group, treat the $ as literal
                            substitute += '$';
                        }
                    }
                    index = replacement.indexOf('$', position);
                }
                substitute += replacement.substring(position);
                return substitute;
            }
            */
        }

        /**
        Signature: $eval(expr [, context])
        Parses and evaluates the string expr which contains literal JSON or a JSONata expression using the current context as the context for evaluation.         
        Optionally override the context by specifying the second parameter
         */
        public static JToken eval([PropagateUndefined] string expr, [AllowContextAsValue] JToken context)
        {
            JsonataQuery query = new JsonataQuery(expr);
            return query.Eval(context);    //TODO: think of using bindings from current environment (custom bindings). Also propagating time from parentevaluationEnvironment
        }

        /**
          Signature: $base64encode()
          Converts an ASCII string to a base 64 representation. 
          Each each character in the string is treated as a byte of binary data. 
          This requires that all characters in the string are in the 0x00 to 0xFF range, which includes all characters in URI encoded strings. 
          Unicode characters outside of that range are not supported.         
         */
        public static string base64encode([AllowContextAsValue][PropagateUndefined] string str)
        {
            return Convert.ToBase64String(UTF8_NO_BOM.GetBytes(str));
        }

        /**
          $base64decode()
          Signature: $base64decode()
          Converts base 64 encoded bytes to a string, using a UTF-8 Unicode codepage.        
         */
        public static string base64decode([AllowContextAsValue][PropagateUndefined] string str)
        {
            return UTF8_NO_BOM.GetString(Convert.FromBase64String(str));
        }

        /**
        $encodeUrlComponent()
        Signature: $encodeUrlComponent(str)
        Encodes a Uniform Resource Locator(URL) component by replacing each instance of certain characters by one, two, three, or four escape sequences representing the UTF-8 encoding of the character.
        */
        public static string encodeUrlComponent([AllowContextAsValue][PropagateUndefined] string str)
        {
            //return WebUtility.UrlEncode(str);
            return Uri.EscapeDataString(str);
        }

        /**
        $encodeUrl()
        Signature: $encodeUrl(str)
        Encodes a Uniform Resource Locator (URL) by replacing each instance of certain characters by one, two, three, or four escape sequences representing the UTF-8 encoding of the character.
        */
        public static string encodeUrl([AllowContextAsValue][PropagateUndefined] string str)
        {
            //see https://stackoverflow.com/a/34189188/376066 and 
            //    https://stackoverflow.com/questions/4396598/whats-the-difference-between-escapeuristring-and-escapedatastring/34189188#comment81544744_34189188
#pragma warning disable SYSLIB0013
            return Uri.EscapeUriString(str);
#pragma warning restore
        }


        /**
        $decodeUrlComponent()
        Signature: $decodeUrlComponent(str)
        Decodes a Uniform Resource Locator (URL) component previously created by encodeUrlComponent.
        */
        public static string decodeUrlComponent([AllowContextAsValue][PropagateUndefined] string str)
        {
            //return WebUtility.UrlEncode(str);
            return Uri.UnescapeDataString(str);
        }

        /**
        $decodeUrl()
        Signature: $decodeUrl(str)
        Decodes a Uniform Resource Locator (URL) previously created by encodeUrl.
        */
        public static string decodeUrl([AllowContextAsValue][PropagateUndefined] string str)
        {
            return Uri.UnescapeDataString(str); //there's no Uri.UnescapeUriString, but actually - what's the difference? 
            // see https://stackoverflow.com/questions/747641/what-is-the-difference-between-decodeuricomponent-and-decodeuri#comment116291569_747700
        }

        #endregion

        #region Numeric functions
        /**
         Signature: $number(arg)
         Casts the arg parameter to a number using the following casting rules
         If arg is not specified (i.e. this function is invoked with no arguments), then the context value is used as the value of arg.
        */
        public static JToken number([AllowContextAsValue] JToken arg)
        {
            switch (arg.Type)
            {
            case JTokenType.Undefined:
                // undefined inputs always return undefined
                return arg;
            case JTokenType.Integer:
                //Numbers are unchanged
                return arg;
            case JTokenType.Float:
                //Numbers are unchanged
                return arg;
            case JTokenType.String:
                //Strings that contain a sequence of characters that represent a legal JSON number are converted to that number
                {
                    string str = (string)arg!;
                    if (Int64.TryParse(str, out long longValue))
                    {
                        return new JValue(longValue);
                    }
                    else if (Double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue))
                    {
                        if (!Double.IsNaN(doubleValue) && !Double.IsInfinity(doubleValue))
                        {
                            long doubleAsLongValue = (long)doubleValue;
                            if (doubleAsLongValue == doubleValue)
                            {
                                //support for 1e5 cases
                                return new JValue(doubleAsLongValue);
                            }
                            else
                            {
                                return new JValue(doubleValue);
                            }
                        }
                        else
                        {
                            throw new JsonataException("D3030", "Jsonata does not support NaNs or Infinity values");
                        }
                    }
                    else
                    {
                        throw new JsonataException("D3030", $"Failed to parse string to number: '{str}'");
                    }
                };
            case JTokenType.Boolean:
                //
                return new JValue((bool)arg ? 1 : 0);
            default:
                //All other values cause an error to be thrown.
                throw new JsonataException("D3030", $"Unable to cast value to a number. Value type is {arg.Type}");
            }
        }


        /**
         Signature: $abs(number)
         Returns the absolute value of the number parameter, i.e. if the number is negative, it returns the positive value.
        
         If number is not specified (i.e. this function is invoked with no arguments), then the context value is used as the value of number.
         */
        public static double abs([AllowContextAsValue][PropagateUndefined] double number)
        {
            return Math.Abs(number);
        }

        /**
         Signature: $floor(number)
         Returns the value of number rounded down to the nearest integer that is smaller or equal to number.
         
         If number is not specified (i.e. this function is invoked with no arguments), then the context value is used as the value of number.
         */
        public static long floor([AllowContextAsValue][PropagateUndefined] double number)
        {
            return (long)Math.Floor(number);
        }

        /**
         Signature: $ceil(number)
         Returns the value of number rounded up to the nearest integer that is greater than or equal to number
         
         If number is not specified (i.e. this function is invoked with no arguments), then the context value is used as the value of number.
         */
        public static long ceil([AllowContextAsValue][PropagateUndefined] double number)
        {
            return (long)Math.Ceiling(number);
        }

        /**
         Signature: $round(number [, precision])

         Returns the value of the number parameter rounded to the number of decimal places specified by the optional precision parameter.

         The precision parameter (which must be an integer) species the number of decimal places to be present in the rounded number. 
         If precision is not specified then it defaults to the value 0 and the number is rounded to the nearest integer. 
         If precision is negative, then its value specifies which column to round to on the left side of the decimal place

         This function uses the Round half to even strategy to decide which way to round numbers that fall exactly between two candidates at the specified precision. 
         This strategy is commonly used in financial calculations and is the default rounding mode in IEEE 754.         
         */
        public static decimal round([AllowContextAsValue][PropagateUndefined] decimal number, [OptionalArgument(0)] int precision)
        {
            //This function uses decimal because Math.Round for double in C# does not exactly follow the expectations because of binary arithmetics issues
            if (precision >= 0)
            {
                return Math.Round(number, precision, MidpointRounding.ToEven);
            }
            else
            {
                precision = -precision;
                int power = (int)Math.Pow(10, precision);
                number = Math.Round(number / power, 0, MidpointRounding.ToEven);
                number *= power;
                return number;
            }
        }

        /**
         Signature: $power(base, exponent)
         Returns the value of base raised to the power of exponent (base ^ exponent).
         If base is not specified (i.e. this function is invoked with one argument), then the context value is used as the value of base.
         An error is thrown if the values of base and exponent lead to a value that cannot be represented as a JSON number (e.g. Infinity, complex numbers).
         */
        public static double power([AllowContextAsValue][PropagateUndefined] double @base, double exponent)
        {
            return Math.Pow(@base, exponent);
        }

        /**
        Signature: $sqrt(number)
        Returns the square root of the value of the number parameter.
        If number is not specified (i.e. this function is invoked with one argument), then the context value is used as the value of number.
        An error is thrown if the value of number is negative.         
        */
        public static double sqrt([AllowContextAsValue][PropagateUndefined] double number)
        {
            return Math.Sqrt(number);
        }

        /**
        Signature: $random()
        Returns a pseudo random number greater than or equal to zero and less than one (0 ≤ n < 1)
        */
        public static double random([EvalSupplementArgument] EvaluationSupplement evalEnv)
        {
            return evalEnv.Random.NextDouble();
        }


        /**
        Signature: $formatNumber(number, picture [, options])
        Casts the number to a string and formats it to a decimal representation as specified by the picture string.
        The behaviour of this function is consistent with the XPath/XQuery function fn:format-number as defined in the XPath F&O 3.1 specification. 
        The picture string parameter defines how the number is formatted and has the same syntax as fn:format-number.
        The optional third argument options is used to override the default locale specific formatting characters such as the decimal separator. 
        If supplied, this argument must be an object containing name/value pairs specified in the decimal format section of the XPath F&O 3.1 specification.         
         */
        public static string formatNumber([AllowContextAsValue][PropagateUndefined] double number, string picture, [OptionalArgument(null)] JObject? options)
        {
            //TODO: try implementing or using proper XPath fn:format-number
            picture = Regex.Replace(picture, @"[1-9]", "0");
            picture = Regex.Replace(picture, @",", @"\,");
            return number.ToString(picture, CultureInfo.InvariantCulture);
        }

        /**
        Signature: $formatBase(number [, radix])
        Casts the number to a string and formats it to an integer represented in the number base specified by the radix argument.
        If radix is not specified, then it defaults to base 10. radix can be between 2 and 36, otherwise an error is thrown.         
        */
        public static string formatBase([AllowContextAsValue][PropagateUndefined] long number, [OptionalArgument(10)] int radix)
        {
            if (radix < 2 || radix > 36)
            {
                throw new JsonataException("D3100", $"The radix of the {nameof(formatBase)} function must be between 2 and 36. It was given {radix}");
            };

            //TODO: implement properly
            if (number < 0)
            {
                return "-" + formatBase(-number, radix);
            };
            switch (radix)
            {
            case 2:
            case 8:
            case 10:
            case 16:
                return Convert.ToString(number, radix);
            default:
                throw new NotImplementedException($"No support for radix={radix} in {nameof(formatBase)}() yet");
            }
        }

        /**
         Signature: $formatInteger(number, picture)
         Casts the number to a string and formats it to an integer representation as specified by the picture string.
         The behaviour of this function is consistent with the two-argument version of the XPath/XQuery function fn:format-integer as defined in the XPath F&O 3.1 specification. 
         The picture string parameter defines how the number is formatted and has the same syntax as fn:format-integer.
         */
        public static string formatInteger([AllowContextAsValue][PropagateUndefined] long number, string picture)
        {
            //TODO: try implementing or using proper XPath fn:format-integer
            //picture = Regex.Replace(picture, @"[1-9]", "0");
            //picture = Regex.Replace(picture, @",", @"\,");
            return number.ToString(picture, CultureInfo.InvariantCulture);
        }

        /**
         Signature: $parseInteger(string, picture)
         Parses the contents of the string parameter to an integer (as a JSON number) using the format specified by the picture string. 
         The picture string parameter has the same format as $formatInteger. 
         Although the XPath specification does not have an equivalent function for parsing integers, this capability has been added to JSONata.
         */
        public static long parseInteger([AllowContextAsValue][PropagateUndefined] string str, [OptionalArgument(null)] string? picture)
        {
            //TODO: try implementing properly
            return Int64.Parse(str);
        }
        #endregion

        #region Numeric aggregation functions
        /**
         Signature: $sum(array)
         Returns the arithmetic sum of an array of numbers. 
         It is an error if the input array contains an item which isn't a number.
         */
        public static JToken sum([PropagateUndefined] JToken arg)
        {
            switch (arg.Type)
            {
            case JTokenType.Integer:
            case JTokenType.Float:
                return arg;
            case JTokenType.Array:
                //continue handling below
                break;
            default:
                throw new JsonataException("T0410", $"Argument 1 of function {nameof(sum)} should be an array of numbers, but specified {arg.Type}");
            }

            decimal result = Helpers.EnumerateNumericArray((JArray)arg, nameof(sum), 1).Sum();
            return FunctionToken.ReturnDecimalResult(result);
        }

        /**
        Signature: $max(array)
        Returns the maximum number in an array of numbers. 
        It is an error if the input array contains an item which isn't a number.
         */
        public static JToken max([PropagateUndefined] JToken arg)
        {
            switch (arg.Type)
            {
            case JTokenType.Integer:
            case JTokenType.Float:
                return arg;
            case JTokenType.Array:
                //continue handling below
                break;
            default:
                throw new JsonataException("T0410", $"Argument 1 of function {nameof(max)} should be an array of numbers, but specified {arg.Type}");
            }

            decimal result = Decimal.MinValue;
            bool found = false;
            foreach (decimal value in Helpers.EnumerateNumericArray((JArray)arg, nameof(max), 1))
            {
                if (value > result)
                {
                    result = value;
                }
                found = true;
            };

            if (!found)
            {
                return JsonataQ.UNDEFINED;
            }

            return FunctionToken.ReturnDecimalResult(result);
        }

        /**
        Signature: min(array)
        Returns the minimum number in an array of numbers.
        It is an error if the input array contains an item which isn't a number.
        */
        public static JToken min([PropagateUndefined] JToken arg)
        {
            switch (arg.Type)
            {
            case JTokenType.Integer:
            case JTokenType.Float:
                return arg;
            case JTokenType.Array:
                //continue handling below
                break;
            default:
                throw new JsonataException("T0410", $"Argument 1 of function {nameof(min)} should be an array of numbers, but specified {arg.Type}");
            }

            decimal result = Decimal.MaxValue;
            bool found = false;
            foreach (decimal value in Helpers.EnumerateNumericArray((JArray)arg, nameof(min), 1))
            {
                if (value < result)
                {
                    result = value;
                }
                found = true;
            };

            if (!found)
            {
                return JsonataQ.UNDEFINED;
            }

            return FunctionToken.ReturnDecimalResult(result);
        }

        /**
        Signature: $average(array)
        Returns the mean value of an array of numbers. It is an error if the input array contains an item which isn't a number.         
         */
        public static JToken average([PropagateUndefined] JToken arg)
        {
            switch (arg.Type)
            {
            case JTokenType.Integer:
            case JTokenType.Float:
                return arg;
            case JTokenType.Array:
                //continue handling below
                break;
            default:
                throw new JsonataException("T0410", $"Argument 1 of function {nameof(average)} should be an array of numbers, but specified {arg.Type}");
            }

            decimal result = 0;
            int count = 0;
            foreach (decimal value in Helpers.EnumerateNumericArray((JArray)arg, nameof(average), 1))
            {
                result += value;
                ++count;
            };

            if (count == 0)
            {
                return JsonataQ.UNDEFINED;
            };
            return FunctionToken.ReturnDecimalResult(result / count);
        }
        #endregion

        #region Boolean functions
        /**
         Signature: $boolean(arg)
         Casts the argument to a Boolean using the following rules: http://docs.jsonata.org/boolean-functions#boolean 
         */
        public static JToken boolean([AllowContextAsValue] JToken arg)
        {
            switch (arg.Type)
            {
            case JTokenType.Boolean:    
                //Boolean:	unchanged
                return arg;
            case JTokenType.String:
                //string: empty   false
                //string: non-empty   true
                return new JValue(((string)arg!) != "");
            case JTokenType.Integer:
                //number: 0	false
                //number: non-zero    true
                return new JValue(((long)arg) != 0);
            case JTokenType.Float:
                //number: 0	false
                //number: non-zero    true
                return new JValue(((double)arg) != 0);
            case JTokenType.Null:
                //null	false
                return new JValue(false);
            case JTokenType.Array:
                //array: empty	false
                //array: contains a member that casts to true true
                //array: all members cast to false    false
                foreach (JToken child in ((JArray)arg).ChildrenTokens)
                {
                    JToken childRes = BuiltinFunctions.boolean(child);
                    if (childRes.Type == JTokenType.Boolean && (bool)childRes)
                    {
                        return new JValue(true);
                    }
                }
                return new JValue(false);
            case JTokenType.Object:
                //object: empty   false
                //object: non-empty   true
                return new JValue(((JObject)arg).Count > 0);
            case JTokenType.Function:
                //function	false
                return new JValue(false);
            case JTokenType.Undefined:
                return arg;
            default:
                throw new ArgumentException("Unexpected arg type: " + arg.Type);
            }
        }

        /**
         Signature: $not(arg)
         Returns Boolean NOT on the argument. arg is first cast to a boolean
         */
        public static JToken not([AllowContextAsValue] JToken arg)
        {
            arg = BuiltinFunctions.boolean(arg);
            if (arg.Type == JTokenType.Undefined)
            {
                return arg;
            }
            return new JValue(!(bool)arg);
        }

        /**
        $exists()
        Signature: $exists(arg)
        Returns Boolean true if the arg expression evaluates to a value, or false if the expression does not match anything (e.g. a path to a non-existent field reference).         
        */
        public static bool exists(JToken arg)
        {
            return arg.Type != JTokenType.Undefined;
        }
        #endregion

        #region Array functions
        /**
         Signature: $count(array)
         Returns the number of items in the array parameter. 
          If the array parameter is not an array, but rather a value of another JSON type, then the parameter is treated as a singleton array containing that value, and this function returns 1.
         If array is not specified, then the context value is used as the value of array         
         */
        public static int count([AllowContextAsValue] JToken arg)
        {
            switch (arg.Type)
            {
            case JTokenType.Undefined:
                return 0;
            case JTokenType.Array:
                return ((JArray)arg).Count;
            default:
                return 1;
            }
        }

        /**
        Signature: $append(array1, array2)

        Returns an array containing the values in array1 followed by the values in array2. 
        If either parameter is not an array, then it is treated as a singleton array containing that value.         
         */
        public static JToken append(JToken arg1, JToken arg2)
        {
            // disregard undefined args
            if (arg1.Type == JTokenType.Undefined)
            {
                return arg2;
            }
            else if (arg2.Type == JTokenType.Undefined)
            {
                return arg1;
            };
            // if either argument is not an array, make it so
            JArray result = new JArray();   //TODO: maybe create it as a sequence? check original jsonata-js append!
            if (arg1 is JArray array1)
            {
                result.AddAll(array1.ChildrenTokens);
            }
            else
            {
                result.Add(arg1);
            }
            if (arg2 is JArray array2)
            {
                result.AddAll(array2.ChildrenTokens);
            }
            else
            {
                result.Add(arg2);
            };
            return result;
        }

        /**
        Signature: $sort(array [, function])

        Returns an array containing all the values in the array parameter, but sorted into order. 
        If no function parameter is supplied, then the array parameter must contain only numbers or only strings, and they will be sorted in order of increasing number, or increasing unicode codepoint respectively.

        If a comparator function is supplied, then is must be a function that takes two parameters: function(left, right)
        This function gets invoked by the sorting algorithm to compare two values left and right. 
        If the value of left should be placed after the value of right in the desired sort order, then the function must return Boolean true to indicate a swap. Otherwise it must return false.         
         */
        public static JArray sort([PropagateUndefined] JToken arrayToken, [OptionalArgument(null)] JToken? function)
        {
            if (arrayToken.Type != JTokenType.Array)
            {
                JsonataArray singletonArray = JsonataArray.CreateSequence();
                singletonArray.keepSingleton = true;
                singletonArray.Add(arrayToken);
                return singletonArray;
            }

            JArray array = (JArray)arrayToken;
            if (array.Count <= 1)
            {
                return array;
            }

            System.Comparison<JToken> comparator;

            if (function == null || function.Type == JTokenType.Undefined)
            {
                if (Helpers.IsArrayOfNumbers(array))
                {
                    comparator = (a, b) => Helpers.GetDoubleValue(a).CompareTo(Helpers.GetDoubleValue(b));
                }
                else if (Helpers.IsArrayOfStrings(array))
                {
                    comparator = (a, b) => String.CompareOrdinal((string)a!, (string)b!);
                }
                else
                {
                    throw new JsonataException("D3070", $"The single argument form of the {nameof(sort)} function can only be applied to an array of strings or an array of numbers.  Use the second argument to specify a comparison function");
                }
            }
            else if (function.Type == JTokenType.Function)
            {
                throw new NotImplementedException();
                /*
                comparator = (a, b) => {
                    JToken res = EvalProcessor.InvokeFunction(
                        function: (FunctionToken)function,
                        args: new List<JToken>() { a, b },
                        context: null,
                        env: null! //TODO: pass some real environment?
                    );
                    bool result = Helpers.Booleanize(res);
                    return result ? 1 : -1; //may cause problems because of no zero (
                };
                */
            }
            else
            {
                //TODO: get proper code
                throw new JsonataException("????", $"Argument 2 of function {nameof(sort)} should be a function(left, right) returning boolean");
            }

            List<JToken> tokens = array.ChildrenTokens.ToList();
            tokens.Sort(comparator);
            JsonataArray result = JsonataArray.CreateSequence();
            result.AddRange(tokens);
            return result;
        }

        // // Implements the merge sort (stable) with optional comparator function
        // 
        // this is a port of jsonata-js fn.sort() which is not that performant (
        internal static JArray sort_internal(JToken arrayToken, Func<JToken, JToken, int> comparator)
        {
            //TODO: think of Undefined (

            //// undefined inputs always return undefined
            //if (arrayToken.Type == JTokenType.Undefined)
            //{
            //    return arrayToken;
            //}

            if (arrayToken.Type != JTokenType.Array)
            {
                JsonataArray singletonArray = JsonataArray.CreateSequence();
                singletonArray.keepSingleton = true;
                singletonArray.Add(arrayToken);
                return singletonArray;
            }

            JArray array = (JArray)arrayToken;
            if (array.Count <= 1)
            {
                return array;
            }

            void merge_iter(JToken[] result, int offset, ArraySegment<JToken> left, ArraySegment<JToken> right) 
            {
                if (left.Count == 0)
                {
                    foreach (JToken token in right)
                    {
                        result[offset++] = token;
                    }
                }
                else if (right.Count == 0)
                {
                    foreach (JToken token in left)
                    {
                        result[offset++] = token;
                    }
                }
                else if (comparator(left[0], right[0]) > 0) // invoke the comparator function
                { 
                    // if it returns true - swap left and right
                    result[offset++] = right[0];
                    merge_iter(result, offset, left, new ArraySegment<JToken>(right.Array!, right.Offset + 1, right.Count - 1));
                }
                else
                {
                    // otherwise keep the same order
                    result[offset++] = left[0];
                    merge_iter(result, offset, new ArraySegment<JToken>(left.Array!, left.Offset + 1, left.Count - 1), right);
                }
            }

            JToken[] msort(ArraySegment<JToken> array) 
            {
                JToken[] result = new JToken[array.Count];
                if (array.Count == 0)
                {
                    return result;
                }
                else if (array.Count == 1)
                {
                    result[0] = array[0];
                    return result;
                }
                else
                {
                    int middle = array.Count / 2;
                    ArraySegment<JToken> left = new ArraySegment<JToken>(array.Array!, array.Offset + 0, middle);
                    ArraySegment<JToken> right = new ArraySegment<JToken>(array.Array!, array.Offset + middle, array.Count - middle);
                    JToken[] leftArray = msort(left);
                    JToken[] rightArray = msort(right);
                    merge_iter(result, 0, new ArraySegment<JToken>(leftArray), new ArraySegment<JToken>(rightArray));
                    return result;
                }
            }

            JToken[] result = msort(new ArraySegment<JToken>(array.ChildrenTokens.ToArray()));
            JArray resultArray = new JArray(result.Length);
            foreach (JToken token in result)
            {
                resultArray.Add(token);
            }
            return resultArray;
        }

        /**
         $reverse()
         Signature: $reverse(array)
         Returns an array containing all the values from the array parameter, but in reverse order.
         */
        public static JArray reverse([PropagateUndefined] JToken arrayToken)
        {
            if (arrayToken.Type != JTokenType.Array)
            {
                JsonataArray singletonArray = JsonataArray.CreateSequence();
                singletonArray.Add(arrayToken);
                return singletonArray;
            }

            JArray array = (JArray)arrayToken;
            if (array.Count <= 1)
            {
                return array;
            }

            JArray result = new JArray(array.Count);
            for (int i = array.Count - 1; i >= 0; --i)
            {
                result.Add(array.ChildrenTokens[i]);
            }
            return result;
        }

        /**
         $shuffle()
         Signature: $shuffle(array)
         Returns an array containing all the values from the array parameter, but shuffled into random order.
         */
        public static JArray shuffle([PropagateUndefined] JToken arrayToken, [EvalSupplementArgument] EvaluationSupplement evalEnv)
        {
            if (arrayToken.Type != JTokenType.Array)
            {
                JsonataArray singletonArray = JsonataArray.CreateSequence();
                singletonArray.Add(arrayToken);
                return singletonArray;
            }

            JArray array = (JArray)arrayToken;
            if (array.Count <= 1)
            {
                return array;
            }
            JToken[] arr = new JToken[array.Count];
            for (int i = 0; i < array.Count; ++i)
            {
                arr[i] = array.ChildrenTokens[i]; 
            }
            for (int i = 0; i < arr.Length; ++i)
            {
                int j = evalEnv.Random.Next(i, arr.Length);
                if (i != j)
                {
                    JToken tmp = arr[i];
                    arr[i] = arr[j];
                    arr[j] = tmp;
                }
            }
            JArray result = new JArray(arr.Length);
            for (int i = 0; i < arr.Length; ++i)
            {
                result.Add(arr[i]);
            }

            return result;
        }

        /**
          $distinct()
          Signature $distinct(array)
          Returns an array containing all the values from the array parameter, but with any duplicates removed. 
          Values are tested for deep equality as if by using the equality operator.
        */
        public static JArray distinct([PropagateUndefined] JToken arrayToken)
        {
            if (arrayToken.Type != JTokenType.Array)
            {
                JsonataArray singletonArray = JsonataArray.CreateSequence();
                singletonArray.Add(arrayToken);
                return singletonArray;
            }

            JArray array = (JArray)arrayToken;
            if (array.Count <= 1)
            {
                return array;
            }

            JArray result = new JArray();
            foreach (JToken item in array.ChildrenTokens)
            {
                bool exists = false;
                foreach (JToken existing in result.ChildrenTokens)
                {
                    if (DeepEquals(item, existing))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    result.Add(item);
                }
            }
            return result;

            bool DeepEquals(JToken a, JToken b)
            {
                if (a.Type == JTokenType.Integer && b.Type == JTokenType.Float)
                {
                    a = new JValue((double)(int)a);
                }
                else if (b.Type == JTokenType.Integer && b.Type == JTokenType.Float)
                {
                    b = new JValue((double)(int)b);
                };
                return JToken.DeepEquals(a, b);
            }
        }


        /**
        Signature: $zip(array1, ...)
        Returns a convolved (zipped) array containing grouped arrays of values from the array1 ... arrayN arguments from index 0, 1, 2, etc.
        This function accepts a variable number of arguments. 
        The length of the returned array is equal to the length of the shortest array in the arguments.         
        */
        public static JArray zip([VariableNumberArgumentAsArray] JArray args)
        {
            JArray result = new JArray();
            int maxLength = int.MaxValue;
            List<JArray> argsList = new List<JArray>(args.Count);
            foreach (JToken arg in args.ChildrenTokens)
            {
                JArray arrayArg;
                switch (arg.Type)
                {
                case JTokenType.Undefined:
                    return result; //undefined has length of 0
                case JTokenType.Array:
                    arrayArg = (JArray)arg;
                    break;
                default:
                    arrayArg = new JArray(1);    //length of 1
                    arrayArg.Add(arg);
                    break;
                }
                argsList.Add(arrayArg);
                if (arrayArg.Count < maxLength)
                {
                    maxLength = arrayArg.Count;
                }
            };

            for (int i = 0; i < maxLength; ++i)
            {
                JArray tuple = new JArray();
                foreach (JArray arrayArg in argsList)
                {
                    tuple.Add(arrayArg.ChildrenTokens[i]);
                };
                result.Add(tuple);
            };

            return result;
        }

        #endregion

        #region Object functions
        /**
         Signature: $keys(object)
         Returns an array containing the keys in the object. 
         If the argument is an array of objects, then the array returned contains a de-duplicated list of all the keys in all of the objects         
         */
        public static JToken keys([AllowContextAsValue][PropagateUndefined] JToken arg)
        {
            ICollection<string> keys;
            switch (arg.Type)
            {
            case JTokenType.Object:
                keys = ((JObject)arg).Keys;
                break;
            case JTokenType.Array:
                keys = ((JArray)arg).ChildrenTokens
                        .OfType<JObject>()
                        .SelectMany(o => o.Keys)
                        .Distinct()
                        .ToList();
                break;
            default:
                return JsonataQ.UNDEFINED;
            }
            if (keys.Count == 0)
            {
                return JsonataQ.UNDEFINED;
            }
            else if (keys.Count == 1)
            {
                return new JValue(keys.First()!);
            };
            JArray result = new JArray(keys.Count);
            foreach (string key in keys)
            {
                result.Add(new JValue(key));
            }
            return result;
        }

        /**
         Signature: $lookup(object, key)
         Returns the value associated with key in object. If the first argument is an array of objects, then all of the objects in the array are searched, and the values associated with all occurrences of key are returned.         
         */
        public static JToken lookup(JToken arg, string key)
        {
            switch (arg.Type)
            {
            case JTokenType.Array:
                {
                    JArray result = JsonataArray.CreateSequence();
                    foreach (JToken child in ((JArray)arg).ChildrenTokens)
                    {
                        JToken res = lookup(child, key);
                        if (res.Type == JTokenType.Array)
                        {
                            result.AddRange(((JArray)res).ChildrenTokens);
                        }
                        else if (res.Type != JTokenType.Undefined)
                        {
                            result.Add(res);
                        }
                    }
                    return result;
                };
            case JTokenType.Object:
                {
                    JObject obj = (JObject)arg;
                    if (obj.Properties.TryGetValue(key, out JToken? result))
                    {
                        return result;
                    }
                    else
                    {
                        return JsonataQ.UNDEFINED;
                    };
                };
            default:
                return JsonataQ.UNDEFINED;

            }
        }

        /**
         Signature: $spread(object)
         Splits an object containing key/value pairs into an array of objects, each of which has a single key/value pair from the input object. 
         If the parameter is an array of objects, then the resultant array contains an object for every key/value pair in every object in the supplied array.
         */
        public static JToken spread([AllowContextAsValue][PropagateUndefined] JToken arg)
        {
            switch (arg.Type)
            {
            case JTokenType.Object:
                {
                    JObject obj = (JObject)arg;
                    if (obj.Count == 0)
                    {
                        return JsonataQ.UNDEFINED;
                    }
                    JArray result = new JArray(obj.Properties.Count);
                    foreach (KeyValuePair<string, JToken> property in obj.Properties)
                    {
                        JObject subResult = new JObject();
                        subResult.Add(property.Key, property.Value);
                        result.Add(subResult);
                    };
                    return result;
                }
            case JTokenType.Array:
                {
                    JArray array = (JArray)arg;
                    if (array.Count == 0)
                    {
                        return JsonataQ.UNDEFINED;
                    }
                    JArray result = new JArray();
                    foreach (JToken element in array.ChildrenTokens)
                    {
                        JToken elementResult = spread(element);
                        switch (elementResult.Type)
                        {
                        case JTokenType.Undefined:
                            break;
                        case JTokenType.Array:
                            result.AddRange(((JArray)elementResult).ChildrenTokens);
                            break;
                        default:
                            result.Add(elementResult);
                            break;
                        }
                    }
                    return result;
                }
            default:
                return arg;
            }
        }

        /**
         Signature: $merge(array<object>)
         Merges an array of objects into a single object containing all the key/value pairs from each of the objects in the input array. 
         If any of the input objects contain the same key, then the returned object will contain the value of the last one in the array. It is an error if the input array contains an item that is not an object.         
         */
        public static JObject merge([AllowContextAsValue][PropagateUndefined] JToken arg)
        {
            switch (arg.Type)
            {
            case JTokenType.Object:
                return (JObject)arg;
            case JTokenType.Array:
                break;
            default:
                throw new JsonataException("T0412", $"Argument 1 of function \"{nameof(merge)}\" must be an array of \"objects\"");
            };
            JArray array = (JArray)arg;
            JObject result = new JObject();
            foreach (JToken element in array.ChildrenTokens)
            {
                switch (element.Type)
                {
                case JTokenType.Undefined:
                    break;
                case JTokenType.Object:
                    {
                        JObject obj = (JObject)element;
                        foreach (KeyValuePair<string, JToken> property in obj.Properties)
                        {
                            result.Set(property.Key, property.Value);
                        };
                    }
                    break;
                default:
                    throw new JsonataException("T0412", $"Argument 1 of function \"{nameof(merge)}\" must be an array of \"objects\"");
                }
            }
            return result;
        }

        /**
         Signature: $each(object, function)
         Returns an array containing the values return by the function when applied to each key/value pair in the object.
         The function parameter will get invoked with two arguments:
            function(value, name)
         where the value parameter is the value of each name/value pair in the object and name is its name. The name parameter is optional.* 
         */
        public static JArray each([AllowContextAsValue][PropagateUndefined] JObject obj, FunctionToken function)
        {
            throw new NotImplementedException();
            /*
            int argsCount = function.RequiredArgsCount;
            JsonataArray result = JsonataArray.CreateSequence();
            foreach (KeyValuePair<string, JToken> prop in obj.Properties)
            {
                List<JToken> args = new List<JToken>();
                if (argsCount >= 1)
                {
                    args.Add(prop.Value);
                };
                if (argsCount >= 2)
                {
                    args.Add(new JValue(prop.Key));
                };
                JToken res = EvalProcessor.InvokeFunction(
                    function: function,
                    args: args,
                    context: null,
                    env: null! //TODO: pass some real environment?
                );
                if (res.Type != JTokenType.Undefined)
                {
                    result.Add(res);
                };
            }
            return result;
            */
        }

        /**
         $error()
         Signature:$error(message)
         Deliberately throws an error with an optional message
         */
        public static JToken error([OptionalArgument(null)] string message)
        {
            throw new JsonataException("D3137", message ?? "$error() function evaluated");
        }

        /**
         Signature:$assert(condition, message)
         If condition is true, the function returns undefined. 
         If the condition is false, an exception is thrown with the message as the message of the exception.         
         */
        public static JToken assert(bool condition, [OptionalArgument(null)] string message)
        {
            if (!condition)
            {
                if (string.IsNullOrEmpty(message))
                {
                    message = "$assert() statement failed";
                };
                throw new JsonataAssertFailedException(message);
            }
            else
            {
                return JsonataQ.UNDEFINED;
            }
        }

        /**
         Signature:$type(value)
        Evaluates the type of value and returns one of the following strings:
            "null"
            "number"
            "string"
            "boolean"
            "array"
            "object"
            "function" 
        Returns (non-string) undefined when value is undefined.
         */
        public static string @type([PropagateUndefined] JToken value)
        {
            switch (value.Type)
            {
            case JTokenType.Null:
                return "null";
            case JTokenType.Integer:
            case JTokenType.Float:
                return "number";
            case JTokenType.String:
                return "string";
            case JTokenType.Boolean:
                return "boolean";
            case JTokenType.Array:
                return "array";
            case JTokenType.Object:
                return "object";
            case JTokenType.Function:
                return "function";
            default:
                throw new Exception("Unexpected JToken type " + value.Type);
            }
        }
        #endregion

        #region Date/Time functions

        internal const string UTC_FORMAT = @"yyyy-MM-dd\THH:mm:ss.fffK";

        /**
         Signature: $now([picture [, timezone]])
         Generates a UTC timestamp in ISO 8601 compatible format and returns it as a string. All invocations of $now() within an evaluation of an expression will all return the same timestamp value.
         If the optional picture and timezone parameters are supplied, then the current timestamp is formatted as described by the $fromMillis() function.         
         */
        public static string now([OptionalArgument(UTC_FORMAT)] string picture, [OptionalArgument(null)] string? timezone, [EvalSupplementArgument] EvaluationSupplement evalEnv)
        {
            return fromMillis(millis(evalEnv), picture, timezone);
        }

        /**
         $millis()
         Signature: $millis()
         Returns the number of milliseconds since the Unix Epoch (1 January, 1970 UTC) as a number. 
         All invocations of $millis() within an evaluation of an expression will all return the same value.
         */
        public static long millis([EvalSupplementArgument] EvaluationSupplement evalEnv)
        {
            return evalEnv.Now.ToUnixTimeMilliseconds();
        }

        /**
         $fromMillis()
         Signature: $fromMillis(number [, picture [, timezone]])
         Convert the number representing milliseconds since the Unix Epoch (1 January, 1970 UTC) to a formatted string representation of the timestamp as specified by the picture string.
         If the optional picture parameter is omitted, then the timestamp is formatted in the ISO 8601 format.
         If the optional picture string is supplied, then the timestamp is formatted occording to the representation specified in that string. The behaviour of this function is consistent with the two-argument version of the XPath/XQuery function fn:format-dateTime as defined in the XPath F&O 3.1 specification. The picture string parameter defines how the timestamp is formatted and has the same syntax as fn:format-dateTime.
         If the optional timezone string is supplied, then the formatted timestamp will be in that timezone. The timezone string should be in the format "±HHMM", where ± is either the plus or minus sign and HHMM is the offset in hours and minutes from UTC. Positive offset for timezones east of UTC, negative offset for timezones west of UTC.         
         */
        public static string fromMillis([PropagateUndefined] long number, [OptionalArgument(UTC_FORMAT)] string picture, [OptionalArgument(null)] string? timezone)
        {
            DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(number);
            if (timezone != null)
            {
                if (!Int32.TryParse(timezone, out int offsetHhMm))
                {
                    throw new JsonataException("D3134", $"Failed to parse timezone offset value from '{timezone}'");
                }
                date = date.ToOffset(new TimeSpan(offsetHhMm / 100, offsetHhMm % 100, 0));
            }
            //see how "K" different for DateTime and DateTimeOffset https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings#KSpecifier
            return (new DateTime(date.Ticks, DateTimeKind.Utc)).ToString(picture);
        }

        /**
         $toMillis()
         Signature: $toMillis(timestamp [, picture])
         Convert a timestamp string to the number of milliseconds since the Unix Epoch (1 January, 1970 UTC) as a number.
         If the optional picture string is not specified, then the format of the timestamp is assumed to be ISO 8601. 
         An error is thrown if the string is not in the correct format.
         If the picture string is specified, then the format is assumed to be described by this picture string using the same syntax as the XPath/XQuery function fn:format-dateTime, defined in the XPath F&O 3.1 specification.         
         */
        public static long toMillis([PropagateUndefined] string timestamp, [OptionalArgument(null)] string? picture)
        {
            DateTimeOffset result;
            if (picture == null)
            {
                if (!DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result))
                {
                    throw new JsonataException("D3136", $"Failed to parse date/time from '{timestamp}'");
                }
            }
            else
            {
                if (!DateTimeOffset.TryParseExact(timestamp, picture, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result))
                {
                    throw new JsonataException("D3136", $"Failed to parse date/time from '{timestamp}' using picture format '{picture}'");
                }
            }

            return result.ToUnixTimeMilliseconds();
        }
        #endregion

        #region Higher order functions

        private static bool FilterAcceptsElement(FunctionToken function, JToken element, int index, JArray array)
        {
            throw new NotImplementedException();
            /*
            int filterArgsCount = function.RequiredArgsCount;
            List<JToken> args = new List<JToken>();
            if (filterArgsCount >= 1)
            {
                args.Add(element);
            };
            if (filterArgsCount >= 2)
            {
                args.Add(new JValue(index));
            };
            if (filterArgsCount >= 3)
            {
                args.Add(array);
            };
            JToken res = EvalProcessor.InvokeFunction(
                function: function,
                args: args,
                context: null,
                env: null! //TODO: pass some real environment?
            );
            bool result = Helpers.Booleanize(res);
            return result;
            */
        }

        /**
         Signature: $map(array, function)
         Returns an array containing the results of applying the function parameter to each value in the array parameter.
         The function that is supplied as the second parameter must have the following signature:
            function(value [, index [, array]])
         Each value in the input array is passed in as the first parameter in the supplied function. 
         The index (position) of that value in the input array is passed in as the second parameter, if specified. 
         The whole input array is passed in as the third parameter, if specified.
         */
        public static JToken map([PropagateUndefined][PackSingleValueToSequence] JArray array, FunctionToken function)
        {
            throw new NotImplementedException();
            /*

            int funcArgsCount = function.RequiredArgsCount;

            JsonataArray result = JsonataArray.CreateSequence();

            int index = 0;
            foreach (JToken element in array.ChildrenTokens)
            {
                List<JToken> args = new List<JToken>();
                if (funcArgsCount >= 1)
                {
                    args.Add(element);
                };
                if (funcArgsCount >= 2)
                {
                    args.Add(new JValue(index));
                };
                if (funcArgsCount >= 3)
                {
                    args.Add(array);
                };
                JToken res = EvalProcessor.InvokeFunction(
                    function: function,
                    args: args,
                    context: null,
                    env: null! //TODO: pass some real environment?
                );
                if (res.Type != JTokenType.Undefined)
                {
                    result.Add(res);
                };
                ++index;
            }
            return result;
            */
        }


        /**
        Signature: $filter(array, function)
        Returns an array containing only the values in the array parameter that satisfy the function predicate (i.e. function returns Boolean true when passed the value).
        The function that is supplied as the second parameter must have the following signature:
            function(value [, index [, array]])
        Each value in the input array is passed in as the first parameter in the supplied function. 
        The index (position) of that value in the input array is passed in as the second parameter, if specified. 
        The whole input array is passed in as the third parameter, if specified.         
         */
        public static JToken filter([PropagateUndefined][PackSingleValueToSequence] JArray array, FunctionToken function)
        {
            JsonataArray result = JsonataArray.CreateSequence();
            int index = 0;
            foreach (JToken element in array.ChildrenTokens)
            {
                if (FilterAcceptsElement(function, element, index, array))
                {
                    result.Add(element);
                }
                ++index;
            }
            //return result.Simplify();
            return result;
        }

        /**
          Signature: $single(array, function)
          Returns the one and only one value in the array parameter that satisfy the function predicate (i.e. function returns Boolean true when passed the value).
          Throws an exception if the number of matching values is not exactly one.

          The function that is supplied as the second parameter must have the following signature:
            function(value [, index [, array]])

          Each value in the input array is passed in as the first parameter in the supplied function. 
          The index (position) of that value in the input array is passed in as the second parameter, if specified. 
          The whole input array is passed in as the third parameter, if specified.         
         */
        public static JToken single([PropagateUndefined][PackSingleValueToSequence] JArray array, [OptionalArgument(null)] FunctionToken? function)
        {
            JToken? result = null;
            int index = 0;
            foreach (JToken element in array.ChildrenTokens)
            {
                bool filterPassed = function != null ? 
                    FilterAcceptsElement(function, element, index, array) 
                    : true;
                if (filterPassed)
                {
                    if (result != null)
                    {
                        throw new JsonataException("D3138", "The $single() function expected exactly 1 matching result.  Instead it matched more.");
                    }
                    else
                    {
                        result = element;
                    }
                }
                ++index;
            }
            
            if (result == null)
            {
                throw new JsonataException("D3139", "The $single() function expected exactly 1 matching result.  Instead it matched 0.");
            }
            return result;
        }

        /**
          Signature: $reduce(array, function [, init])
          Returns an aggregated value derived from applying the function parameter successively to each value in array in combination with the result of the previous application of the function.
          The function must accept at least two arguments, and behaves like an infix operator between each value within the array. 
          The signature of this supplied function must be of the form:
            myfunc($accumulator, $value[, $index[, $array]])         
          If the optional init parameter is supplied, then that value is used as the initial value in the aggregation (fold) process. 
          If not supplied, the initial value is the first value in the array parameter.
         */
        public static JToken reduce([PropagateUndefined][PackSingleValueToSequence] JArray array, FunctionToken function, [OptionalArgument(null)] JToken? init)
        {
            throw new NotImplementedException();
            /*
            JToken accumulator;
            IEnumerable<JToken> elements;
            int index;
            if (init == null || init.Type == JTokenType.Undefined)
            {
                if (array.Count == 0)
                {
                    return JsonataQ.UNDEFINED;
                };
                accumulator = array.ChildrenTokens[0];
                elements = array.ChildrenTokens.Skip(1);
                index = 1;
            }
            else
            {
                accumulator = init;
                elements = array.ChildrenTokens;
                index = 0;
            };

            int funcArgsCount = function.RequiredArgsCount;
            if (funcArgsCount < 2)
            {
                throw new JsonataException("D3050", "The second argument of reduce function must be a function with at least two arguments");
            }

            foreach (JToken element in elements)
            {
                List<JToken> args = new List<JToken>(funcArgsCount);
                args.Add(accumulator);
                args.Add(element);
                if (funcArgsCount >= 3)
                {
                    args.Add(new JValue(index));
                };
                if (funcArgsCount >= 4)
                {
                    args.Add(array);
                };
                accumulator = EvalProcessor.InvokeFunction(
                    function: function,
                    args: args,
                    context: null,
                    env: null! //TODO: pass some real environment?
                );
                ++index;
            }
            return accumulator;
            */
        }

        /**
         Signature: $sift(object, function)
         Returns an object that contains only the key/value pairs from the object parameter that satisfy the predicate function passed in as the second parameter.
         If object is not specified, then the context value is used as the value of object.
         It is an error if object is not an object.

         The function that is supplied as the second parameter must have the following signature:
            function(value [, key [, object]])
         Each value in the input object is passed in as the first parameter in the supplied function. 
         The key (property name) of that value in the input object is passed in as the second parameter, if specified. 
         The whole input object is passed in as the third parameter, if specified.
         */
        public static JToken sift([AllowContextAsValue][PropagateUndefined] JObject obj, FunctionToken function)
        {
            throw new NotImplementedException();

            /*
            JObject result = new JObject();
            foreach (KeyValuePair<string, JToken> property in obj.Properties)
            {
                if (filterAcceptsElement(property.Value, property.Key, obj))
                {
                    result.Add(property.Key, property.Value);
                }
            }
            if (result.Count == 0)
            {
                return JsonataQ.UNDEFINED;
            }
            return result;

            bool filterAcceptsElement(JToken value, string key, JObject obj)
            {
                List<JToken> args = new List<JToken>();
                int filterArgsCount = function.RequiredArgsCount;
                if (filterArgsCount >= 1)
                {
                    args.Add(value);
                };
                if (filterArgsCount >= 2)
                {
                    args.Add(new JValue(key));
                };
                if (filterArgsCount >= 3)
                {
                    args.Add(obj);
                };
                JToken res = EvalProcessor.InvokeFunction(
                    function: function,
                    args: args,
                    context: null,
                    env: null! //TODO: pass some real environment?
                );
                bool result = Helpers.Booleanize(res);
                return result;
            }
            */
        }


        #endregion


    }
}

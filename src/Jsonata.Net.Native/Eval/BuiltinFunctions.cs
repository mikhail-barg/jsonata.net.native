using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    internal static class BuiltinFunctions
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
                    return new JValue(arg.ToString(Formatting.None));
                };
            case FunctionToken.TYPE:
                //Functions are converted to an empty string
                return new JValue("");
            default:
                return new JValue(arg.ToString(formatting: prettify ? Formatting.Indented : Formatting.None));
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
        public static bool contains([AllowContextAsValue][PropagateUndefined] string str, string pattern)
        {
            //TODO: support RegExes!!
            return str.Contains(pattern);
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
        public static JArray split([AllowContextAsValue][PropagateUndefined] string str, string separator, [OptionalArgument(100000000)] int limit)
        {
            //TODO: support RegExes!!

            if (limit < 0)
            {
                throw new JsonataException("D3020", $"Third argument of {nameof(split)} function must evaluate to a positive number. Passed {limit}");
            }

            JArray result = new JArray();
            if (separator == "")
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
                foreach (string part in Regex.Split(str, Regex.Escape(separator)))
                {
                    if (result.Count >= limit)
                    {
                        break;
                    }
                    result.Add(new JValue(part));
                }
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
                foreach (JToken element in array.Children())
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
        public static double random([EvalEnvironmentArgument] EvaluationEnvironment evalEnv)
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
                throw new JsonataException("T0410", $"Arguent 1 of function {nameof(sum)} should be array of numbers, but specified {arg.Type}");
            }

            if (arg.Children().All(t => (t.Type == JTokenType.Integer || t.Type == JTokenType.Undefined)))
            {
                //eval to int
                long result = 0;
                foreach (JToken token in arg.Children())
                {
                    if (token.Type != JTokenType.Undefined)
                    {
                        result += (long)token;
                    }
                }
                return new JValue(result);
            }
            else
            {
                //eval to double
                double result = 0;
                foreach (JToken token in arg.Children())
                {
                    switch (token.Type)
                    {
                    case JTokenType.Integer:
                        result += (long)token;
                        break;
                    case JTokenType.Float:
                        result += (double)token;
                        break;
                    case JTokenType.Undefined:
                        //just skip
                        break;
                    default:
                        throw new JsonataException("T0412", $"Argument of function {nameof(sum)} must be an array of numbers. Got {token.Type}");
                    }
                }
                return new JValue(result);
            }
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
                return ((string)arg!) != "";
            case JTokenType.Integer:
                //number: 0	false
                //number: non-zero    true
                return ((long)arg) != 0;
            case JTokenType.Float:
                //number: 0	false
                //number: non-zero    true
                return ((double)arg) != 0;
            case JTokenType.Null:
                //null	false
                return false;
            case JTokenType.Array:
                //array: empty	false
                //array: contains a member that casts to true true
                //array: all members cast to false    false
                foreach (JToken child in arg.Children())
                {
                    JToken childRes = BuiltinFunctions.boolean(child);
                    if (childRes.Type == JTokenType.Boolean && (bool)childRes)
                    {
                        return true;
                    }
                }
                return false;
            case JTokenType.Object:
                //object: empty   false
                //object: non-empty   true
                return arg.HasValues;
            case FunctionToken.TYPE:
                //function	false
                return false;
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
            return !(bool)arg;
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
                return arg.Count();
            default:
                return 1;
            }
        }

        /**
        Signature: $append(array1, array2)

        Returns an array containing the values in array1 followed by the values in array2. 
        If either parameter is not an array, then it is treated as a singleton array containing that value.         
         */
        public static JToken append(JToken array1, JToken array2)
        {
            // disregard undefined args
            if (array1.Type == JTokenType.Undefined)
            {
                return array2;
            }
            else if (array2.Type == JTokenType.Undefined)
            {
                return array1;
            };
            // if either argument is not an array, make it so
            JArray result = new Sequence();
            if (array1.Type == JTokenType.Array)
            {
                result.AddRange(array1.Children());
            }
            else
            {
                result.Add(array1);
            };
            if (array2.Type == JTokenType.Array)
            {
                result.AddRange(array2.Children());
            }
            else
            {
                result.Add(array2);
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
                Sequence singletonArray = new Sequence();
                singletonArray.keepSingletons = true;
                singletonArray.Add(arrayToken);
                return singletonArray;
            }

            JArray array = (JArray)arrayToken;
            if (array.Count == 0)
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
            else if (function.Type == FunctionToken.TYPE)
            {
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
            }
            else
            {
                //TODO: get proper code
                throw new JsonataException("????", $"Argument 2 of function {nameof(sort)} should be a function(left, right) returning boolean");
            }

            List<JToken> tokens = array.Children().ToList();
            tokens.Sort(comparator);
            JArray result = new Sequence();
            result.AddRange(tokens);
            return result;
        }

        #endregion

        #region Object functions
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
                    JArray result = new Sequence();
                    foreach (JToken child in arg.Children())
                    {
                        JToken res = lookup(child, key);
                        if (res.Type == JTokenType.Array)
                        {
                            result.AddRange(res.Children());
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
                    if (obj.TryGetValue(key, out JToken? result))
                    {
                        return result;
                    }
                    else
                    {
                        return EvalProcessor.UNDEFINED;
                    };
                };
            default:
                return EvalProcessor.UNDEFINED;

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
            case FunctionToken.TYPE:
                return "function";
            default:
                throw new Exception("Unexpected JToken type " + value.Type);
            }
        }
        #endregion
    }
}

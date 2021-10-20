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
        #region Numeric functions
        /**
         Signature: $number(arg)
         Casts the arg parameter to a number using the following casting rules
         */
        //TODO: * If arg is not specified (i.e. this function is invoked with no arguments), then the context value is used as the value of arg.
        //["1", "2", "3", "4", "5"].$number() => [1, 2, 3, 4, 5]
        public static JToken number(JToken arg)
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
        
         TODO: If number is not specified (i.e. this function is invoked with no arguments), then the context value is used as the value of number.
         */

        public static double abs([PropagateUndefined] double number)
        {
            return Math.Abs(number);
        }

        /**
         Signature: $floor(number)
         Returns the value of number rounded down to the nearest integer that is smaller or equal to number.
         
         TODO: If number is not specified (i.e. this function is invoked with no arguments), then the context value is used as the value of number.
         */
        public static long floor([PropagateUndefined] double number)
        {
            return (long)Math.Floor(number);
        }

        /**
         Signature: $ceil(number)
         Returns the value of number rounded up to the nearest integer that is greater than or equal to number
         
         TODO: If number is not specified (i.e. this function is invoked with no arguments), then the context value is used as the value of number.
         */
        public static long ceil([PropagateUndefined] double number)
        {
            return (long)Math.Ceiling(number);
        }

        /**
         Signature: $round(number [, precision])

         Returns the value of the number parameter rounded to the number of decimal places specified by the optional precision parameter.

         The precision parameter (which must be an integer) species the number of decimal places to be present in the rounded number. If precision is not specified then it defaults to the value 0 and the number is rounded to the nearest integer. If precision is negative, then its value specifies which column to round to on the left side of the decimal place

         This function uses the Round half to even strategy to decide which way to round numbers that fall exactly between two candidates at the specified precision. This strategy is commonly used in financial calculations and is the default rounding mode in IEEE 754.         
         */
        public static decimal round([PropagateUndefined] decimal number, [OptionalArgument(0)] int precision)
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
         TODO: If base is not specified (i.e. this function is invoked with one argument), then the context value is used as the value of base.
         An error is thrown if the values of base and exponent lead to a value that cannot be represented as a JSON number (e.g. Infinity, complex numbers).
         */
        public static double power([PropagateUndefined] double @base, double exponent)
        {
            return Math.Pow(@base, exponent);
        }

        /**
        Signature: $sqrt(number)
        Returns the square root of the value of the number parameter.
        TODO: If number is not specified (i.e. this function is invoked with one argument), then the context value is used as the value of number.
        An error is thrown if the value of number is negative.         
        */
        public static double sqrt([PropagateUndefined] double number)
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
        The optional third argument options is used to override the default locale specific formatting characters such as the decimal separator. If supplied, this argument must be an object containing name/value pairs specified in the decimal format section of the XPath F&O 3.1 specification.         
         */
        public static string formatNumber([PropagateUndefined] double number, string picture, [OptionalArgument(null)] JObject? options)
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
        public static string formatBase([PropagateUndefined] long number, [OptionalArgument(10)] int radix)
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
        public static string formatInteger([PropagateUndefined] long number, string picture)
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
        public static long parseInteger([PropagateUndefined] string str, [OptionalArgument(null)] string? picture)
        {
            //TODO: try implementing properly
            return Int64.Parse(str);
        }
        #endregion

        #region Numeric aggregation functions
        /**
         Signature: $sum(array)
         Returns the arithmetic sum of an array of numbers. It is an error if the input array contains an item which isn't a number.
         */
        public static JToken sum(JArray arg)
        {
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
        public static JToken boolean(JToken arg)
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
        public static JToken not(JToken arg)
        {
            arg = BuiltinFunctions.boolean(arg);
            if (arg.Type == JTokenType.Undefined)
            {
                return arg;
            }
            return !(bool)arg;
        }
        #endregion
    }
}

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
                            throw new JsonataException("D3030", "Jsonata doe not support NaNs or Infinity values");
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

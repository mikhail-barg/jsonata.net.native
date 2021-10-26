using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    internal static class Helpers
    {
        public static void AddRange(this JArray array, IEnumerable<JToken> values)
        {
            foreach (JToken value in values)
            {
                array.Add(value);
            }
        }

        public static bool IsArrayOfNumbers(JToken token)
        {
            if (token.Type != JTokenType.Array)
            {
                return false;
            }
            foreach (JToken subtoken in ((JArray)token).Children())
            {
                if (subtoken.Type != JTokenType.Integer && subtoken.Type != JTokenType.Float)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsArrayOfStrings(JToken token)
        {
            if (token.Type != JTokenType.Array)
            {
                return false;
            }
            foreach (JToken subtoken in ((JArray)token).Children())
            {
                if (subtoken.Type != JTokenType.String)
                {
                    return false;
                }
            }
            return true;
        }

        public static double GetDoubleValue(JToken token)
        {
            switch (token.Type)
            {
            case JTokenType.Float:
                return (double)token;
            case JTokenType.Integer:
                return (double)(long)token;
            default:
                throw new Exception("Not a number " + token.ToString(Formatting.None));
            }
        }

        //TODO: think of using BuiltinFunctions.boolean
        public static bool Booleanize(JToken value)
        {
            // cast arg to its effective boolean value
            // boolean: unchanged
            // string: zero-length -> false; otherwise -> true
            // number: 0 -> false; otherwise -> true
            // null -> false
            // array: empty -> false; length > 1 -> true
            // object: empty -> false; non-empty -> true
            // function -> false

            switch (value.Type)
            {
            case JTokenType.Undefined:
                return false;
            case JTokenType.Array:
                {
                    JArray array = (JArray)value;
                    if (array.Count == 0)
                    {
                        return false;
                    }
                    else if (array.Count == 1)
                    {
                        return Helpers.Booleanize(array.Children().First());
                    }
                    else
                    {
                        return array.Children().Any(c => Helpers.Booleanize(c));
                    }
                };
            case JTokenType.String:
                return ((string)value!).Length > 0;
            case JTokenType.Integer:
                return ((long)value) != 0;
            case JTokenType.Float:
                return ((double)value) != 0.0;
            case JTokenType.Object:
                return ((JObject)value!).Count > 0;
            case JTokenType.Boolean:
                return (bool)value;
            case FunctionToken.TYPE:
                return false;
            default:
                return false;
            }
        }
    }
}

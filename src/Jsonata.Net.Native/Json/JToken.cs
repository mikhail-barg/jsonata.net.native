using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    [DebuggerDisplay("{Type}: {ToStringFlat()}")]
    public abstract class JToken
    {
        public readonly JTokenType Type;

        protected JToken(JTokenType type)
        {
            this.Type = type;
        }

        private static JValue AsValue(JToken token)
        {
            if (token is JValue value)
            {
                return value;
            }
            throw new Exception("Token is not a JValue");
        }

        public static explicit operator int(JToken value)
        {
            JValue v = AsValue(value);
            if (v.Type != JTokenType.Integer)
            {
                throw new Exception("Cannot convert to int");
            }
            return Convert.ToInt32(v.Value, CultureInfo.InvariantCulture);
        }

        public static explicit operator long(JToken value)
        {
            JValue v = AsValue(value);
            if (v.Type != JTokenType.Integer)
            {
                throw new Exception("Cannot convert to long");
            }
            return Convert.ToInt64(v.Value, CultureInfo.InvariantCulture);
        }

        public static explicit operator decimal(JToken value)
        {
            JValue v = AsValue(value);
            if (v.Type != JTokenType.Float)
            {
                throw new ArgumentException("Can not convert to Decimal");
            }

            return Convert.ToDecimal(v.Value, CultureInfo.InvariantCulture);
        }

        public static explicit operator double(JToken value)
        {
            JValue v = AsValue(value);
            if (v.Type != JTokenType.Float)
            {
                throw new ArgumentException("Can not convert to Double");
            }

            return Convert.ToDouble(v.Value, CultureInfo.InvariantCulture);
        }

        public static explicit operator float(JToken value)
        {
            JValue v = AsValue(value);
            if (v.Type != JTokenType.Float)
            {
                throw new ArgumentException("Can not convert to Double");
            }

            return Convert.ToSingle(v.Value, CultureInfo.InvariantCulture);
        }

        public static explicit operator string(JToken value)
        {
            JValue v = AsValue(value);
            if (v.Type != JTokenType.String)
            {
                throw new ArgumentException("Can not convert to String");
            }

            return Convert.ToString(v.Value, CultureInfo.InvariantCulture)!;
        }

        public static explicit operator bool(JToken value)
        {
            JValue v = AsValue(value);
            if (v.Type != JTokenType.Boolean)
            {
                throw new ArgumentException("Can not convert to Bool");
            }

            return Convert.ToBoolean(v.Value, CultureInfo.InvariantCulture);
        }

        

        public static JToken FromObject(object? sourceObj)
        {
            switch (sourceObj)
            {
            case null:
                return JValue.CreateNull();
            case bool value:
                return new JValue(value);
            case string value:
                return new JValue(value);
            case char value:
                return new JValue(value);
            case int value:
                return new JValue(value);
            case long value:
                return new JValue(value);
            case float value:
                return new JValue(value);
            case double value:
                return new JValue(value);
            case decimal value:
                return new JValue(value);
            case System.Collections.IDictionary dictionary:
                return FromDictionary(dictionary);
            case System.Collections.ICollection list:
                return FromCollection(list);
            default:
                return FromObj(sourceObj);
            }
        }

        private static JToken FromObj(object sourceObj)
        {
            JObject result = new JObject();
            foreach (PropertyInfo pi in sourceObj.GetType().GetProperties())
            {
                result.Add(pi.Name, JToken.FromObject(pi.GetValue(sourceObj)));
            }
            return result;
        }

        private static JToken FromCollection(ICollection list)
        {
            JArray array = new JArray(list.Count);
            foreach (object? item in list)
            {
                array.Add(JToken.FromObject(item));
            }
            return array;
        }

        private static JToken FromDictionary(IDictionary dictionary)
        {
            JObject result = new JObject();
            foreach (DictionaryEntry entry in dictionary)
            {
                result.Add(entry.Key.ToString()!, JToken.FromObject(entry.Value));
            }
            return result;
        }

        public string ToIndentedString()
        {
            StringBuilder builder = new StringBuilder();
            this.ToIndentedStringImpl(builder, 0);
            return builder.ToString();
        }

        internal abstract void ToIndentedStringImpl(StringBuilder builder, int indent);

        public string ToStringFlat()
        {
            StringBuilder builder = new StringBuilder();
            this.ToStringFlatImpl(builder);
            return builder.ToString();
        }

        internal abstract void ToStringFlatImpl(StringBuilder builder);

        public abstract JToken DeepClone();

        public static bool DeepEquals(JToken lhs, JToken rhs)
        {
            return lhs.DeepEquals(rhs);
        }

        public abstract bool DeepEquals(JToken other);
    }
}

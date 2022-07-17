﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    internal abstract class JToken
    {
        internal readonly JTokenType Type;

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

        internal static JToken FromNewtonsoft(Newtonsoft.Json.Linq.JToken value)
        {
            switch (value.Type)
            {
            case Newtonsoft.Json.Linq.JTokenType.Array:
                {
                    JArray result = new JArray();
                    foreach (Newtonsoft.Json.Linq.JToken child in value.Children())
                    {
                        result.Add(FromNewtonsoft(child));
                    }
                    return result;
                }
            case Newtonsoft.Json.Linq.JTokenType.Boolean:
                return new JValue((bool)value);
            case Newtonsoft.Json.Linq.JTokenType.Float:
                return new JValue((double)value);
            case Newtonsoft.Json.Linq.JTokenType.Integer:
                return new JValue((long)value);
            case Newtonsoft.Json.Linq.JTokenType.Null:
                return JValue.CreateNull();
            case Newtonsoft.Json.Linq.JTokenType.Object:
                {
                    JObject result = new JObject();
                    foreach (Newtonsoft.Json.Linq.JProperty prop in ((Newtonsoft.Json.Linq.JObject)value).Properties())
                    {
                        result.Add(prop.Name, FromNewtonsoft(prop.Value));
                    }
                    return result;
                }
            case Newtonsoft.Json.Linq.JTokenType.String:
                return new JValue((string)value!);
            case Newtonsoft.Json.Linq.JTokenType.Undefined:
                return JValue.CreateUndefined();

            case Newtonsoft.Json.Linq.JTokenType.Bytes:
            case Newtonsoft.Json.Linq.JTokenType.Constructor:
            case Newtonsoft.Json.Linq.JTokenType.Comment:
            case Newtonsoft.Json.Linq.JTokenType.Date:
            case Newtonsoft.Json.Linq.JTokenType.Guid:
            case Newtonsoft.Json.Linq.JTokenType.None:
            case Newtonsoft.Json.Linq.JTokenType.Property:
            case Newtonsoft.Json.Linq.JTokenType.Raw:
            case Newtonsoft.Json.Linq.JTokenType.TimeSpan:
            case Newtonsoft.Json.Linq.JTokenType.Uri:
            default:
                throw new ArgumentException("Weird Token type " + value.Type);
            }
        }

        internal string ToIndentedString()
        {
            StringBuilder builder = new StringBuilder();
            this.ToIndentedString(builder, 0);
            return builder.ToString();
        }

        internal abstract void ToIndentedString(StringBuilder builder, int indent);

        internal string ToStringFlat()
        {
            StringBuilder builder = new StringBuilder();
            this.ToStringFlat(builder);
            return builder.ToString();
        }

        internal abstract void ToStringFlat(StringBuilder builder);

        internal abstract Newtonsoft.Json.Linq.JToken ToNewtonsoft();

        internal abstract JToken DeepClone();

        internal static bool DeepEquals(JToken lhs, JToken rhs)
        {
            return lhs.DeepEquals(rhs);
        }

        internal abstract bool DeepEquals(JToken other);
    }
}

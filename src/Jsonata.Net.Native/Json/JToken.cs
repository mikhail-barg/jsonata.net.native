using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Eval;

namespace Jsonata.Net.Native.Json
{
    [DebuggerDisplay("{Type}: {ToFlatString()}")]
    public abstract class JToken
    {
        public readonly JTokenType Type;

        private JToken? m_parent = null;

        internal JToken? parent
        {
            get => this.m_parent;
            set
            {
                if (this == EvalProcessor.UNDEFINED && value != null)
                {
                    throw new InvalidOperationException(
                        $"Attempt to set parent on {nameof(EvalProcessor)}.{nameof(EvalProcessor.UNDEFINED)}");
                }

                this.m_parent = value;
            }
        }

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


        public static JToken Parse(TextReader reader, ParseSettings? settings = null)
        {
            JsonParser parser = new JsonParser(reader, settings ?? ParseSettings.DefaultSettings);
            return parser.Parse();
        }

        public static JToken Parse(string source, ParseSettings? settings = null)
        {
            using (StringReader reader = new StringReader(source))
            {
                return Parse(reader, settings);
            }
        }

        public static Task<JToken> ParseAsync(TextReader reader, CancellationToken ct, ParseSettings? settings = null)
        {
            JsonParserAsync parser = new JsonParserAsync(reader, settings ?? ParseSettings.DefaultSettings);
            return parser.ParseAsync(ct);
        }

        public static void Validate(TextReader reader, ParseSettings? settings = null)
        {
            JsonParser parser = new JsonParser(reader, settings ?? ParseSettings.DefaultSettings);
            parser.Validate();
        }

        public static void Validate(string source, ParseSettings? settings = null)
        {
            using (StringReader reader = new StringReader(source))
            {
                Validate(reader, settings);
            }
        }

        public static Task ValidateAsync(TextReader reader, CancellationToken ct, ParseSettings? settings = null)
        {
            JsonParserAsync parser = new JsonParserAsync(reader, settings ?? ParseSettings.DefaultSettings);
            return parser.ValidateAsync(ct);
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
                case uint value:
                    return new JValue(value);
                case long value:
                    return new JValue(value);
                case ulong value:
                    return new JValue((decimal)value); //won't fit in (s)long
                case byte value:
                    return new JValue(value);
                case sbyte value:
                    return new JValue(value);
                case short value:
                    return new JValue(value);
                case ushort value:
                    return new JValue(value);
                case float value:
                    return new JValue(value);
                case double value:
                    return new JValue(value);
                case decimal value:
                    return new JValue(value);
                case DateTimeOffset value:
                    return new JValue(value);
                case DateTime value:
                    return new JValue(value);
                case TimeSpan value:
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

        internal void ClearParent()
        {
            this.parent = null;
            this.ClearParentNested();
        }

        protected abstract void ClearParentNested();

        public string ToIndentedString()
        {
            return this.ToIndentedString(SerializationSettings.DefaultSettings);
        }

        public string ToIndentedString(SerializationSettings options)
        {
            StringBuilder builder = new StringBuilder();
            this.ToIndentedStringImpl(builder, 0, options);
            return builder.ToString();
        }

        internal abstract void ToIndentedStringImpl(StringBuilder builder, int indent, SerializationSettings options);

        public string ToFlatString()
        {
            return this.ToFlatString(SerializationSettings.DefaultSettings);
        }

        public string ToFlatString(SerializationSettings options)
        {
            StringBuilder builder = new StringBuilder();
            this.ToStringFlatImpl(builder, options);
            return builder.ToString();
        }

        internal abstract void ToStringFlatImpl(StringBuilder builder, SerializationSettings options);

        public abstract JToken DeepClone();

        public static bool DeepEquals(JToken lhs, JToken rhs)
        {
            return lhs.DeepEquals(rhs);
        }

        public abstract bool DeepEquals(JToken other);

        public T ToObject<T>()
        {
            return this.ToObject<T>(ToObjectSettings.DefaultSettings)!;
        }

        public T ToObject<T>(ToObjectSettings settings)
        {
            return (T)this.ToObject(typeof(T), settings)!;
        }

        public object? ToObject(Type type)
        {
            return this.ToObject(type, ToObjectSettings.DefaultSettings);
        }

        public object? ToObject(Type type, ToObjectSettings settings)
        {
            if (type == typeof(string))
            {
                if (this.Type == JTokenType.Null)
                {
                    return null;
                }

                return (string)this;
            }
            else if (type == typeof(int))
            {
                return (int)this;
            }
            else if (type == typeof(long))
            {
                return (long)this;
            }
            else if (type == typeof(float))
            {
                if (this.Type == JTokenType.Integer)
                {
                    //explicit support, because casts are strict for a reason
                    return (float)(long)this;
                }

                return (float)this;
            }
            else if (type == typeof(double))
            {
                if (this.Type == JTokenType.Integer)
                {
                    //explicit support, because casts are strict for a reason
                    return (double)(long)this;
                }

                return (double)this;
            }
            else if (type == typeof(decimal))
            {
                if (this.Type == JTokenType.Integer)
                {
                    //explicit support, because casts are strict for a reason
                    return (decimal)(long)this;
                }

                return (decimal)this;
            }
            else if (type == typeof(bool))
            {
                return (bool)this;
            }
            else if (Nullable.GetUnderlyingType(type) != null)
            {
                if (this.Type == JTokenType.Null)
                {
                    return null;
                }
                else
                {
                    //wrap value in Nullable<T>
                    Type valueType = Nullable.GetUnderlyingType(type)!;
                    object? value = this.ToObject(valueType, settings);
                    object? result = Activator.CreateInstance(type, new object?[] { value });
                    return result;
                }
            }

            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                if (type.GenericTypeArguments.Length != 2 || type.GenericTypeArguments[0] != typeof(string))
                {
                    throw new ArgumentException($"Cannot convert to dict of type {type.Name}: unexpected generic args");
                }

                Type valueType = type.GenericTypeArguments[1];
                Type resultType = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);

                if (!type.IsAssignableFrom(resultType))
                {
                    throw new ArgumentException(
                        $"Cannot convert to dict of type {type.Name}: not assignable from Dictionary");
                }

                if (this.Type == JTokenType.Null)
                {
                    return null;
                }
                else if (this.Type == JTokenType.Object)
                {
                    return ConvertToDictionary(resultType, valueType, settings);
                }
                else
                {
                    throw new ArgumentException($"Cannot convert {this.Type} to dict {type.Name}");
                }
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                if (type.GenericTypeArguments.Length != 1)
                {
                    throw new ArgumentException($"Cannot convert to list of type {type.Name}: unexpected generic args");
                }

                Type valueType = type.GenericTypeArguments[0];
                Type resultType = typeof(List<>).MakeGenericType(valueType);

                if (!type.IsAssignableFrom(resultType))
                {
                    throw new ArgumentException(
                        $"Cannot convert to list of type {type.Name}: not assignable from List");
                }

                if (this.Type == JTokenType.Null)
                {
                    return null;
                }
                else if (this.Type == JTokenType.Array)
                {
                    return this.ConvertToList(resultType, valueType, settings);
                }
                else
                {
                    throw new ArgumentException($"Cannot convert {this.Type} to array {type.Name}");
                }
            }
            else if (type.IsEnum)
            {
                string? value = (string)this;
#if (NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1)
                if (!Enum.TryParse(type, value, out object? result))
                {
                    throw new ArgumentException($"Failed to parse '{value}' to enum {type.Name}");
                }

                return result;
#else
                try
                {
                    object? result = Enum.Parse(type, value);
                    return result;
                }
                catch (Exception)
                {
                    throw new ArgumentException($"Failed to parse '{value}' to enum {type.Name}");
                }
#endif
            }
            else if (type == typeof(object))
            {
                switch (this.Type)
                {
                    case JTokenType.Object:
                        return this.ConvertToDictionary(
                            typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(object)), typeof(object),
                            settings);
                    case JTokenType.Array:
                        return this.ConvertToList(typeof(List<>).MakeGenericType(typeof(object)), typeof(object),
                            settings);
                    case JTokenType.Integer:
                        return (long)this;
                    case JTokenType.Float:
                        return (double)this;
                    case JTokenType.String:
                        return (string)this;
                    case JTokenType.Boolean:
                        return (bool)this;
                    case JTokenType.Null:
                        return null;
                    default:
                        throw new ArgumentException($"Cannot convert {this.Type} to object");
                }
            }
            else if (type.IsClass)
            {
                if (this.Type == JTokenType.Null)
                {
                    return null;
                }
                else if (this.Type == JTokenType.Object)
                {
                    object result;
                    try
                    {
                        result = Activator.CreateInstance(type)!;
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException($"Failed to create instance of class {type.Name}: {ex.Message}",
                            ex);
                    }

                    Dictionary<string, PropertyInfo> resultProperties = type
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .ToDictionary(pi => pi.Name);

                    JObject thisObj = (JObject)this;

                    foreach (KeyValuePair<string, PropertyInfo> resultProperty in resultProperties)
                    {
                        if (!thisObj.Properties.TryGetValue(resultProperty.Key, out JToken? thisProperty))
                        {
                            if (settings.AllowMissingProperties)
                            {
                                continue;
                            }

                            throw new ArgumentException($"Missing value for '{resultProperty.Key}'");
                        }

                        object? value;
                        try
                        {
                            value = thisProperty.ToObject(resultProperty.Value.PropertyType, settings);
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException(
                                $"Failed to convert value for '{resultProperty.Key}': {ex.Message}", ex);
                        }

                        try
                        {
                            resultProperty.Value.SetValue(result, value);
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException($"Failed to set value for '{resultProperty.Key}': {ex.Message}",
                                ex);
                        }
                    }

                    if (!settings.AllowUndecaredProperties && thisObj.Keys.Except(resultProperties.Keys).Any())
                    {
                        throw new ArgumentException(
                            $"Specified unknown properties: {String.Join(",", thisObj.Keys.Except(resultProperties.Keys))}");
                    }

                    return result;
                }
                else
                {
                    throw new ArgumentException($"Cannot convert {this.Type} to obj {type.Name}");
                }
            }
            else
            {
                throw new ArgumentException($"Cannot convert {this.Type} to some {type.Name}");
            }
        }

        private object ConvertToList(Type listType, Type valueType, ToObjectSettings settings)
        {
            System.Collections.IList result = (System.Collections.IList)Activator.CreateInstance(listType)!;
            foreach (JToken element in ((JArray)this).ChildrenTokens)
            {
                object? value = element.ToObject(valueType, settings);
                result.Add(value);
            }

            return result;
        }

        private object ConvertToDictionary(Type dictionaryType, Type valueType, ToObjectSettings settings)
        {
            System.Collections.IDictionary result =
                (System.Collections.IDictionary)Activator.CreateInstance(dictionaryType)!;
            foreach (KeyValuePair<string, JToken> property in ((JObject)this).Properties)
            {
                object? value = property.Value.ToObject(valueType, settings);
                result.Add(property.Key, value);
            }

            return result;
        }

        //see https://stackoverflow.com/questions/19176024/how-to-escape-special-characters-in-building-a-json-string
        // https://www.freeformatter.com/json-escape.html
        public static void EscapeString(string source, StringBuilder target)
        {
            foreach (char c in source)
            {
                switch (c)
                {
                    case '\b':
                        target.Append(@"\b");
                        break;
                    case '\f':
                        target.Append(@"\f");
                        break;
                    case '\n':
                        target.Append(@"\n");
                        break;
                    case '\r':
                        target.Append(@"\r");
                        break;
                    case '\t':
                        target.Append(@"\t");
                        break;
                    case '"':
                        target.Append("\\\"");
                        break;
                    case '\\':
                        target.Append(@"\\");
                        break;
                    default:
                        target.Append(c);
                        break;
                }
            }
        }
    }
}
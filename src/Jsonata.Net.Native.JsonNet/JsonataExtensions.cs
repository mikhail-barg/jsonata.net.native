using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;

namespace Jsonata.Net.Native.JsonNet
{
    public static class JsonataExtensions
    {
        public static JToken FromNewtonsoft(Newtonsoft.Json.Linq.JToken value)
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
                {
                    decimal decimalValue;
                    try
                    {
                        decimalValue = (decimal)value;
                    }
                    catch (Exception ex)
                    {
                        throw new JsonataException("S0102", $"Number out of range: {value} ({ex.Message})");
                    }
                    return new JValue(decimalValue);
                }
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

            case Newtonsoft.Json.Linq.JTokenType.Date:
                return new JValue(((DateTime)value).ToString());

            case Newtonsoft.Json.Linq.JTokenType.TimeSpan:
                return new JValue(((TimeSpan)value).ToString());

            case Newtonsoft.Json.Linq.JTokenType.Uri:
                return new JValue(((Uri)value!).ToString());

            case Newtonsoft.Json.Linq.JTokenType.Guid:
                return new JValue(((Guid)value).ToString());

            case Newtonsoft.Json.Linq.JTokenType.Bytes:
            case Newtonsoft.Json.Linq.JTokenType.Constructor:
            case Newtonsoft.Json.Linq.JTokenType.Comment:
            case Newtonsoft.Json.Linq.JTokenType.None:
            case Newtonsoft.Json.Linq.JTokenType.Property:
            case Newtonsoft.Json.Linq.JTokenType.Raw:
            default:
                throw new ArgumentException("Weird Token type " + value.Type);
            }
        }

        /**
         * <summary> may be used instead of JToken.FromObject() in case some custom converters are needed</summary>
         */
        public static JToken FromObjectViaNewtonsoft(object? value, Newtonsoft.Json.JsonSerializer? serializer = null)
        {
            if (value == null)
            {
                return JValue.CreateNull();
            };

            Newtonsoft.Json.Linq.JToken token = Newtonsoft.Json.Linq.JToken.FromObject(value, serializer ?? Newtonsoft.Json.JsonSerializer.CreateDefault());
            return JsonataExtensions.FromNewtonsoft(token);
        }

        public static Newtonsoft.Json.Linq.JToken ToNewtonsoft(this JToken value)
        {
            switch (value.Type)
            {
            case JTokenType.Array:
                {
                    JArray source = (JArray)value;
                    Newtonsoft.Json.Linq.JArray result = new Newtonsoft.Json.Linq.JArray();
                    foreach (JToken child in source.ChildrenTokens)
                    {
                        result.Add(ToNewtonsoft(child));
                    }
                    return result;
                }
            case JTokenType.Object:
                {
                    JObject source = (JObject)value;
                    Newtonsoft.Json.Linq.JObject result = new Newtonsoft.Json.Linq.JObject();
                    foreach (KeyValuePair<string, JToken> prop in source.Properties)
                    {
                        result.Add(prop.Key, ToNewtonsoft(prop.Value));
                    }
                    return result;
                }
            case JTokenType.Function:
                throw new NotSupportedException("Not supported for functions");
            case JTokenType.Null:
                return Newtonsoft.Json.Linq.JValue.CreateNull();
            case JTokenType.Undefined:
                return Newtonsoft.Json.Linq.JValue.CreateUndefined();
            case JTokenType.Float:
                return new Newtonsoft.Json.Linq.JValue((decimal)value);
            case JTokenType.Integer:
                return new Newtonsoft.Json.Linq.JValue((long)value);
            case JTokenType.String:
                return Newtonsoft.Json.Linq.JValue.CreateString((string)value);
            case JTokenType.Boolean:
                return new Newtonsoft.Json.Linq.JValue((bool)value);
            default:
                throw new Exception("Unexpected type " + value.Type);
            }
        }

        public static string EvalNewtonsoft(this JsonataQuery query, string dataJson)
        {
            Newtonsoft.Json.Linq.JToken data = Newtonsoft.Json.Linq.JToken.Parse(dataJson);
            JToken result = query.Eval(JsonataExtensions.FromNewtonsoft(data));
            return result.ToIndentedString();
        }

        public static Newtonsoft.Json.Linq.JToken EvalNewtonsoft(this JsonataQuery query, Newtonsoft.Json.Linq.JToken data, Newtonsoft.Json.Linq.JObject? bindings = null)
        {
            EvaluationEnvironment env;
            if (bindings != null)
            {
                env = new EvaluationEnvironment((JObject)JsonataExtensions.FromNewtonsoft(bindings));
            }
            else
            {
                env = EvaluationEnvironment.DefaultEnvironment;
            };
            return EvalNewtonsoft(query, data, env);
        }

        public static Newtonsoft.Json.Linq.JToken EvalNewtonsoft(this JsonataQuery query, Newtonsoft.Json.Linq.JToken data, EvaluationEnvironment environment)
        {
            JToken result = query.Eval(JsonataExtensions.FromNewtonsoft(data), environment);
            return result.ToNewtonsoft();
        }

        public static void BindValue(this EvaluationEnvironment env, string name, Newtonsoft.Json.Linq.JToken value)
        {
            env.BindValue(name, JsonataExtensions.FromNewtonsoft(value));  //allow overrides
        }

    }
}

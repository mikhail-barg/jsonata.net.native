using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;

namespace Jsonata.Net.Native.SystemTextJson
{
    public static class JsonataExtensions
    {
        public static JToken FromSystemTextJson(JsonDocument document)
        {
            return FromSystemTextJson(document.RootElement);
        }

        public static JToken FromSystemTextJson(JsonElement element)
        {
            switch (element.ValueKind)
            {
            case JsonValueKind.Array:
                {
                    JArray result = new JArray(element.GetArrayLength());
                    foreach (JsonElement child in element.EnumerateArray())
                    {
                        result.Add(FromSystemTextJson(child));
                    }
                    return result;
                }
            case JsonValueKind.True:
                return new JValue(true);
            case JsonValueKind.False:
                return new JValue(false);
            case JsonValueKind.Number:
                {
                    if (element.TryGetInt32(out int intValue))
                    {
                        return new JValue(intValue);
                    }
                    else if (element.TryGetInt64(out long longValue))
                    {
                        return new JValue(longValue);
                    }
                    else if (element.TryGetDecimal(out decimal decimalValue))
                    {
                        return new JValue(decimalValue);
                    }
                    else if (element.TryGetDouble(out double doubleValue))
                    {
                        return new JValue(doubleValue);
                    }
                    else
                    {
                        throw new Exception("Failed to parse number from " + element);
                    }
                }
            case JsonValueKind.Null:
                return JValue.CreateNull();
            case JsonValueKind.Object:
                {
                    JObject result = new JObject();
                    foreach (JsonProperty prop in element.EnumerateObject())
                    {
                        result.Add(prop.Name, FromSystemTextJson(prop.Value));
                    }
                    return result;
                }
            case JsonValueKind.String:
                return new JValue(element.GetString()!);
            case JsonValueKind.Undefined:
                return JValue.CreateUndefined();
            default:
                throw new ArgumentException("JsonValueKind " + element.ValueKind);
            }
        }

        //Note that there's no "Writable DOM" for System.Text.Json for now, see https://github.com/dotnet/runtime/pull/34099
        public static JsonDocument ToSystemTextJson(this JToken value)
        {
            return JsonDocument.Parse(value.ToFlatString());
        }

        public static string EvalSystemTextJson(this JsonataQuery query, string dataJson)
        {
            JsonDocument doc = JsonDocument.Parse(dataJson);
            JToken result = query.Eval(FromSystemTextJson(doc));
            return result.ToIndentedString();
        }

        public static JsonDocument EvalSystemTextJson(this JsonataQuery query, JsonDocument data)
        {
            return query.EvalSystemTextJson(data, EvaluationEnvironment.DefaultEnvironment);
        }

        public static JsonDocument EvalSystemTextJson(this JsonataQuery query, JsonDocument data, EvaluationEnvironment environment)
        {
            JToken result = query.Eval(FromSystemTextJson(data), environment);
            return result.ToSystemTextJson();
        }

        public static void BindValue(this EvaluationEnvironment env, string name, JsonElement value)
        {
            env.BindValue(name, FromSystemTextJson(value));  //allow overrides
        }

    }
}

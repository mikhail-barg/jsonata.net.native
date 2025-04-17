using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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

        public static JToken FromSystemTextJson(JsonNode? node)
        {
            //not using node.GetValueKind() because of totally wretched implementation: https://github.com/dotnet/runtime/blob/eeadd653e1982d7037a93a9ab38129c07336e7db/src/libraries/System.Text.Json/src/System/Text/Json/Nodes/JsonValueOfT.cs#L68

            if (node == null)
            {
                return JValue.CreateNull();
            }
            else if (node is JsonArray array)
            {
                JArray result = new JArray(array.Count);
                for (int i = 0; i < array.Count; ++i)
                {
                    JsonNode? child = array[i];
                    result.Add(FromSystemTextJson(child));
                }
                return result;
            }
            else if (node is JsonObject obj)
            {
                JObject result = new JObject();
                foreach (KeyValuePair<string, JsonNode?> prop in obj)
                {
                    result.Add(prop.Key, FromSystemTextJson(prop.Value));
                }
                return result;
            }
            else if (node is JsonValue value)
            {
                if (value.TryGetValue(out bool boolValue))
                {
                    return new JValue(boolValue);
                }
                else if (value.TryGetValue(out int intValue))
                {
                    return new JValue(intValue);
                }
                else if (value.TryGetValue(out long longValue))
                {
                    return new JValue(longValue);
                }
                else if (value.TryGetValue(out decimal decimalValue))
                {
                    return new JValue(decimalValue);
                }
                else if (value.TryGetValue(out double doubleValue))
                {
                    return new JValue(doubleValue);
                }
                else if (value.TryGetValue(out string? strValue))
                {
                    return new JValue(strValue);
                }
                else
                {
                    throw new ArgumentException($"Value {node} is something strange: {node.GetValueKind()}");
                }
            }
            else
            {
                throw new ArgumentException($"Node {node} is something strange: {node.GetValueKind()}");
            }
        }

        //Note that there's no "Writable DOM" for System.Text.Json for now, see https://github.com/dotnet/runtime/pull/34099
        public static JsonDocument ToSystemTextJson(this JToken value)
        {
            return JsonDocument.Parse(value.ToFlatString());
        }

        public static JsonNode? ToSystemTextJsonNode(this JToken value)
        {
            switch (value.Type)
            {
            case JTokenType.Array:
                {
                    JArray source = (JArray)value;
                    JsonArray result = new JsonArray();
                    foreach (JToken child in source.ChildrenTokens)
                    {
                        result.Add(ToSystemTextJsonNode(child));
                    }
                    return result;
                }
            case JTokenType.Object:
                {
                    JObject source = (JObject)value;
                    JsonObject result = new JsonObject();
                    foreach (KeyValuePair<string, JToken> prop in source.Properties)
                    {
                        result.Add(prop.Key, ToSystemTextJsonNode(prop.Value));
                    }
                    return result;
                }
            case JTokenType.Function:
                throw new NotSupportedException("Not supported for functions");
            case JTokenType.Null:
                return null;    //seems there's no JsonValue for Null: https://github.com/dotnet/runtime/blob/eeadd653e1982d7037a93a9ab38129c07336e7db/src/libraries/System.Text.Json/src/System/Text/Json/Nodes/JsonValue.cs#L67
            case JTokenType.Undefined:
                return JsonValue.Create(new JsonElement()); //this would create a node with JsonValueKind.Undefined, see https://github.com/mikhail-barg/jsonata.net.native/issues/38#issuecomment-2813936416 
            case JTokenType.Float:
                try
                {
                    decimal decimalValue = (decimal)value;
                    return JsonValue.Create(decimalValue);
                }
                catch (OverflowException)
                {
                    //throw new JsonataException("S0102", $"Number out of range: {value} ({ex.Message})");
                    return JsonValue.Create((double)value);
                }
            case JTokenType.Integer:
                return JsonValue.Create((long)value);
            case JTokenType.String:
                return JsonValue.Create((string)value);
            case JTokenType.Boolean:
                return JsonValue.Create((bool)value);
            default:
                throw new Exception("Unexpected type " + value.Type);
            }
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

        public static JsonNode? EvalSystemTextJson(this JsonataQuery query, JsonNode data)
        {
            return query.EvalSystemTextJson(data, EvaluationEnvironment.DefaultEnvironment);
        }

        public static JsonNode? EvalSystemTextJson(this JsonataQuery query, JsonNode data, EvaluationEnvironment environment)
        {
            JToken result = query.Eval(FromSystemTextJson(data), environment);
            return result.ToSystemTextJsonNode();
        }

        public static void BindValue(this EvaluationEnvironment env, string name, JsonElement value)
        {
            env.BindValue(name, FromSystemTextJson(value));  //allow overrides
        }

        public static void BindValue(this EvaluationEnvironment env, string name, JsonNode value)
        {
            env.BindValue(name, FromSystemTextJson(value));  //allow overrides
        }

        /**
         * <summary> may be used instead of JToken.FromObject() in case some custom converters are needed</summary>
         */
        public static JToken FromObjectViaSystemTextJson(object? value, JsonSerializerOptions? options = null)
        {
            if (value == null)
            {
                return JValue.CreateNull();
            };

            JsonNode? node = JsonSerializer.SerializeToNode(value, value.GetType(), options);

            return JsonataExtensions.FromSystemTextJson(node);
        }
    }
}

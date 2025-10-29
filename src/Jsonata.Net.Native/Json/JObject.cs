using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    public sealed class JObject : JToken
    {
        private readonly Dictionary<string, JToken> m_properties = new Dictionary<string, JToken>();

        public int Count => this.m_properties.Count;

        public IReadOnlyDictionary<string, JToken> Properties => this.m_properties;
        public ICollection<string> Keys => this.m_properties.Keys;

        public JObject() 
            : base(JTokenType.Object)
        {
        }

        public void Add(string name, JToken value)
        {
            this.m_properties.Add(name, value);
        }

        public void Set(string key, JToken value)
        {
            this.m_properties[key] = value;
        }

        public void Merge(JObject update)
        {
            foreach (KeyValuePair<string, JToken> prop in update.Properties)
            {
                this.m_properties[prop.Key] = prop.Value;
            }
        }

        public void Remove(string key)
        {
            this.m_properties.Remove(key);
        }

        internal bool HasPropertyOfType(string key, JTokenType type)
        {
            if (!this.Properties.TryGetValue(key, out JToken? value))
            {
                return false;
            }
            return value.Type == type;
        }

        internal int GetPropertyAsInt(string key)
        {
            return (int)(JValue)this.Properties[key];
        }

        internal override void ToIndentedStringImpl(StringBuilder builder, int indent, SerializationSettings options)
        {
            builder.Append('{');
            bool serializedSomething = false;
            foreach (KeyValuePair<string, JToken> prop in this.m_properties)
            {
                if (!options.SerializeNullProperties && prop.Value.Type == JTokenType.Null)
                {
                    //skip null properties
                    continue;
                }

                if (serializedSomething)
                {
                    builder.Append(',');
                }
                builder.AppendJsonLine();

                builder.Indent(indent + 1);

                builder.Append('"');
                JToken.EscapeString(prop.Key, builder);
                builder.Append('"').Append(':').Append(' ');
                prop.Value.ToIndentedStringImpl(builder, indent + 1, options);
                serializedSomething = true;
            }

            if (serializedSomething)
            {
                builder.AppendJsonLine();
                builder.Indent(indent);
            }
            builder.Append('}');
        }

        internal override void ToStringFlatImpl(StringBuilder builder, SerializationSettings options)
        {
            builder.Append('{');
            bool serializedSomething = false;
            foreach (KeyValuePair<string, JToken> prop in this.m_properties)
            {
                if (!options.SerializeNullProperties && prop.Value.Type == JTokenType.Null)
                {
                    //skip null properties
                    continue;
                }

                if (serializedSomething)
                {
                    builder.Append(',');
                }

                builder.Append('"');
                JToken.EscapeString(prop.Key, builder);
                builder.Append('"').Append(':');
                prop.Value.ToStringFlatImpl(builder, options);
                serializedSomething = true;
            }
            builder.Append('}');
        }

        public override JToken DeepClone()
        {
            JObject result = new JObject();
            foreach (KeyValuePair<string, JToken> prop in this.m_properties)
            {
                result.Add(prop.Key, prop.Value.DeepClone());
            }
            return result;
        }

        public override bool DeepEquals(JToken other)
        {
            if (this.Type != other.Type)
            {
                return false;
            }
            JObject otherObj = (JObject)other;
            if (this.m_properties.Count != otherObj.m_properties.Count)
            {
                return false;
            }
            foreach (KeyValuePair<string, JToken> prop in this.m_properties)
            {
                if (!otherObj.m_properties.TryGetValue(prop.Key, out JToken? otherValue))
                {
                    return false;
                }
                if (otherValue == null)
                {
                    return false;
                }
                if (!prop.Value.DeepEquals(otherValue))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

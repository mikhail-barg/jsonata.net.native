using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    internal sealed class JObject : JToken
    {
        private readonly Dictionary<string, JToken> m_properties = new Dictionary<string, JToken>();

        internal int Count => this.m_properties.Count;

        public IReadOnlyDictionary<string, JToken> Properties => this.m_properties;
        public ICollection<string> Keys => this.m_properties.Keys;

        internal JObject() 
            : base(JTokenType.Object)
        {
        }

        internal void Add(string name, JToken value)
        {
            this.m_properties.Add(name, value);
        }

        internal void Set(string key, JToken value)
        {
            this.m_properties[key] = value;
        }

        internal void Merge(JObject update)
        {
            foreach (KeyValuePair<string, JToken> prop in update.Properties)
            {
                this.m_properties[prop.Key] = prop.Value;
            }
        }

        internal void Remove(string key)
        {
            this.m_properties.Remove(key);
        }

        internal override void ToIndentedStringImpl(StringBuilder builder, int indent)
        {
            if (this.m_properties.Count == 0)
            {
                builder.Append("{}");
                return;
            }

            builder.Append('{').AppendJsonLine();
            int i = 0;
            foreach (KeyValuePair<string, JToken> prop in this.m_properties)
            {
                builder.Indent(indent + 1);
                builder.Append('"').Append(prop.Key).Append('"').Append(':').Append(' ');
                prop.Value.ToIndentedStringImpl(builder, indent + 1);
                if (i < this.m_properties.Count - 1)
                {
                    builder.Append(',');
                }
                builder.AppendJsonLine();
                ++i;
            }
            builder.Indent(indent);
            builder.Append('}');
        }

        internal override void ToStringFlatImpl(StringBuilder builder)
        {
            builder.Append('{');
            int i = 0;
            foreach (KeyValuePair<string, JToken> prop in this.m_properties)
            {
                builder.Append('"').Append(prop.Key).Append('"').Append(':');
                prop.Value.ToStringFlatImpl(builder);
                if (i < this.m_properties.Count - 1)
                {
                    builder.Append(',');
                }
                ++i;
            }
            builder.Append('}');
        }

        internal override Newtonsoft.Json.Linq.JToken ToNewtonsoft()
        {
            Newtonsoft.Json.Linq.JObject result = new Newtonsoft.Json.Linq.JObject();
            foreach (KeyValuePair<string, JToken> prop in this.m_properties)
            {
                result.Add(prop.Key, prop.Value.ToNewtonsoft());
            }
            return result;
        }

        internal override JToken DeepClone()
        {
            JObject result = new JObject();
            foreach (KeyValuePair<string, JToken> prop in this.m_properties)
            {
                result.Add(prop.Key, prop.Value.DeepClone());
            }
            return result;
        }

        internal override bool DeepEquals(JToken other)
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

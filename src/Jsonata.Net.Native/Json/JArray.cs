using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    internal class JArray : JToken
    {
        private readonly List<JToken> m_values = new List<JToken>();

        internal IReadOnlyList<JToken> ChildrenTokens => this.m_values;
        internal int Count => this.m_values.Count;

        internal JArray() 
            : base(JTokenType.Array)
        {
        }

        internal void Add(JToken token)
        {
            this.m_values.Add(token);
        }

        internal override void ToIndentedString(StringBuilder builder, int indent)
        {
            if (this.m_values.Count == 0)
            {
                builder.Append("[]");
                return;
            }

            builder.Append('[').AppendJsonLine();
            for (int i = 0; i < this.m_values.Count; ++i)
            {
                builder.Indent(indent + 1);
                this.m_values[i].ToIndentedString(builder, indent + 1);
                if (i < this.m_values.Count - 1)
                {
                    builder.Append(',');
                }
                builder.AppendJsonLine();
            }
            builder.Indent(indent);
            builder.Append(']');
        }

        internal override void ToStringFlat(StringBuilder builder)
        {
            builder.Append('[');
            for (int i = 0; i < this.m_values.Count; ++i)
            {
                this.m_values[i].ToStringFlat(builder);
                if (i < this.m_values.Count - 1)
                {
                    builder.Append(',');
                }
            }
            builder.Append(']');
        }

        internal override Newtonsoft.Json.Linq.JToken ToNewtonsoft()
        {
            Newtonsoft.Json.Linq.JArray result = new Newtonsoft.Json.Linq.JArray();
            foreach (JToken token in this.m_values)
            {
                result.Add(token.ToNewtonsoft());
            }
            return result;
        }

        internal override JToken DeepClone()
        {
            JArray result = DeepCloneArrayNoChildren();
            foreach (JToken child in this.m_values)
            {
                result.Add(child.DeepClone());
            }
            return result;
        }

        protected virtual JArray DeepCloneArrayNoChildren()
        {
            return new JArray();
        }

        internal override bool DeepEquals(JToken other)
        {
            if (this.Type != other.Type)
            {
                return false;
            }
            JArray otherArray = (JArray)other;
            if (this.m_values.Count != otherArray.m_values.Count)
            {
                return false;
            }
            for (int i = 0; i < this.m_values.Count; ++i)
            {
                if (!this.m_values[i].DeepEquals(otherArray.m_values[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

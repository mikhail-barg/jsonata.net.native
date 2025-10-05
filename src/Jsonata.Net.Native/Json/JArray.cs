using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    public class JsonataArray: JArray
    {
        public bool sequence { get; set; }
        public bool cons { get; set; }
        public bool keepSingleton { get; set; }
        public bool outerWrapper { get; set; }
        public bool tupleStream { get; set; }

        internal JsonataArray() { }

        internal JsonataArray(IReadOnlyList<JToken> childTokens) 
        { 
            this.AddAll(childTokens);
        }

        public static JsonataArray CreateSequence()
        {
            return new JsonataArray() { sequence = true };
        }

        public static JsonataArray CreateSequence(JToken child)
        {
            JsonataArray result = new JsonataArray() { sequence = true };
            if (child is JArray array && array.Count == 1)
            {
                result.Add(array.ChildrenTokens[0]);
            }
            else
            {
                result.Add(child);
            }
            return result;
        }
    }

    public class JArray : JToken
    {
        private readonly List<JToken> m_values;

        public IReadOnlyList<JToken> ChildrenTokens => this.m_values;
        public int Count => this.m_values.Count;

        public JArray() 
            : base(JTokenType.Array)
        {
            m_values = new List<JToken>();
        }

        public JArray(int capacity)
            : base(JTokenType.Array)
        {
            m_values = new List<JToken>(capacity);
        }

        public void Add(JToken token)
        {
            this.m_values.Add(token);
        }

        public void AddAll(IEnumerable<JToken> tokens)
        {
            foreach (JToken token in tokens)
            {
                this.Add(token);
            }
        }

        protected override void ClearParentNested()
        {
            foreach (JToken child in this.m_values)
            {
                child.ClearParent();
            }
        }

        internal override void ToIndentedStringImpl(StringBuilder builder, int indent, SerializationSettings options)
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
                this.m_values[i].ToIndentedStringImpl(builder, indent + 1, options);
                if (i < this.m_values.Count - 1)
                {
                    builder.Append(',');
                }
                builder.AppendJsonLine();
            }
            builder.Indent(indent);
            builder.Append(']');
        }

        internal override void ToStringFlatImpl(StringBuilder builder, SerializationSettings options)
        {
            builder.Append('[');
            for (int i = 0; i < this.m_values.Count; ++i)
            {
                this.m_values[i].ToStringFlatImpl(builder, options);
                if (i < this.m_values.Count - 1)
                {
                    builder.Append(',');
                }
            }
            builder.Append(']');
        }

        public override JToken DeepClone()
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

        public override bool DeepEquals(JToken other)
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

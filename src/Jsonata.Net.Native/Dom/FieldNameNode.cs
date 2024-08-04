using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Parsing;

namespace Jsonata.Net.Native.Dom
{
    // A NameNode represents a JSON field name.
    public sealed class FieldNameNode : Node
    {
        public string value { get; }
        public bool escaped { get; }

        public FieldNameNode(string value, bool escaped)
        {
            this.value = value;
            this.escaped = escaped;
        }

        public FieldNameNode(string value)
            :this(value, true)
        {
        }

        internal override Node optimize()
        {
            return new PathNode(new List<Node>() { this }, keepArrays: false);
        }

        public override string ToString()
        {
            return this.escaped ?
                "`" + this.value + "`"
                : this.value;
        }

        protected override bool EqualsSpecific(Node other)
        {
            FieldNameNode otherNode = (FieldNameNode)other;
            return this.value == otherNode.value 
                && this.escaped == otherNode.escaped;
        }
    }
}

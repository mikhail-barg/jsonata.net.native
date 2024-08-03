using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Parsing;

namespace Jsonata.Net.Native.Dom
{
    // A NameNode represents a JSON field name.
    public sealed class NameNode : Node
    {
        public string value { get; }
        public bool escaped { get; }

        public NameNode(string value, bool escaped)
        {
            this.value = value;
            this.escaped = escaped;
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
    }
}

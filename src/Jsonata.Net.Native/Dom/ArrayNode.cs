using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Parsing;

namespace Jsonata.Net.Native.Dom
{
    // An ArrayNode represents an array of items.
    public sealed class ArrayNode : Node
    {
        private readonly List<Node> m_items;
        public IReadOnlyList<Node> items => this.m_items;

        public ArrayNode(List<Node> items)
        {
            this.m_items = items ?? throw new ArgumentNullException(nameof(items));
        }

        internal override Node optimize()
        {
            for (int i = 0; i < this.m_items.Count; ++i)
            {
                this.m_items[i] = this.m_items[i].optimize();
            }
            return this;
        }

        public override string ToString()
        {
            return "[" + Helpers.JoinNodes(this.items, ", ") + "]";
        }
    }
}

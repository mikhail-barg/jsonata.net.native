using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    // A DescendentNode represents the descendant operator.
    public sealed class DescendantNode : Node
    {
        public DescendantNode() { }

        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return "**";
        }

        protected override bool EqualsSpecific(Node other)
        {
            return true;
        }
    }
}

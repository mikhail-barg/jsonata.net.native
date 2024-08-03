using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    // A DescendentNode represents the descendant operator.
    public sealed class DescendentNode : Node
    {
        public DescendentNode()
        {

        }

        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return "**";
        }
    }
}

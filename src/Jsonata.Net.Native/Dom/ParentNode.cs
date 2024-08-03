using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Parsing;

namespace Jsonata.Net.Native.Dom
{
    // A ParentNode represents a parent loockback.
    public sealed class ParentNode : Node
    {
        public ParentNode() { }

        internal override Node optimize()
        {
            return new PathNode(new List<Node>() { this }, keepArrays: false);
        }

        public override string ToString()
        {
            return "%";
        }
    }
}

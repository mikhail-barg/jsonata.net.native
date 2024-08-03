using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    // A PlaceholderNode represents a placeholder argument
    // in a partially applied function.
    public sealed class PlaceholderNode : Node
    {
        public PlaceholderNode() { }

        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return "?";
        }
    }
}

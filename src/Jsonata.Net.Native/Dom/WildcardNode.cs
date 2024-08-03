using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    // A WildcardNode represents the wildcard operator.
    public sealed class WildcardNode() : Node
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return "*";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public sealed class NullNode : Node
    {
        public NullNode() { }

        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return "null";
        }

        protected override bool EqualsSpecific(Node other)
        {
            return true;
        }
    }
}

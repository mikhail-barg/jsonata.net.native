using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public sealed class BooleanNode : Node
    {
        public bool value { get; }

        public BooleanNode(bool value)
        {
            this.value = value;
        }

        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return this.value.ToString();
        }
    }
}

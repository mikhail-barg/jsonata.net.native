using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public sealed class StringNode : Node
    {
        public string value { get; }

        public StringNode(string value) 
        { 
            this.value = value;
        }

        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return this.value;
        }

        protected override bool EqualsSpecific(Node other)
        {
            StringNode otherNode = (StringNode)other;
            return otherNode.value == this.value;
        }
    }

}

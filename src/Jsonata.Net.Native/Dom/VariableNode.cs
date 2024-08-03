using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    // A VariableNode represents a JSONata variable.
    public sealed class VariableNode : Node
    {
        public string name { get; }

        public VariableNode(string name)
        {
            this.name = name;
        }

        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return "$" + this.name;
        }
    }
}

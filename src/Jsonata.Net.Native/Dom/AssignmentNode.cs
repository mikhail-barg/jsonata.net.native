using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    // An AssignmentNode represents a variable assignment.
    public sealed class AssignmentNode : Node
    {
        public string name { get; }
        public Node value { get; }

        public AssignmentNode(string name, Node value) 
        {
            this.name = name;
            this.value = value;
        }

        internal override Node optimize()
        {
            Node value = this.value.optimize();
            if (value != this.value)
            {
                return new AssignmentNode(this.name, value);
            }
            else
            {
                return this;
            }
        }
    }
}

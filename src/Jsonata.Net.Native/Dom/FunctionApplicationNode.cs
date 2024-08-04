using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    // A FunctionApplicationNode represents a function application
    // operation.
    public sealed class FunctionApplicationNode : Node
    {
        public Node lhs { get; }
        public Node rhs { get; }

        public FunctionApplicationNode(Node lhs, Node rhs)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        internal override Node optimize()
        {
            Node lhs = this.lhs.optimize();
            Node rhs = this.rhs.optimize();
            if (lhs != this.lhs || rhs != this.rhs)
            {
                return new FunctionApplicationNode(lhs, rhs);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            return $"{this.lhs} ~> {this.rhs}";
        }

        protected override bool EqualsSpecific(Node other)
        {
            FunctionApplicationNode otherNode = (FunctionApplicationNode)other;

            return this.lhs.Equals(otherNode.lhs)
                && this.rhs.Equals(otherNode.rhs);
        }
    }
}

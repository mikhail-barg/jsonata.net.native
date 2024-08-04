using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    // A NegationNode represents a numeric negation operation.
    public sealed class NegationNode : Node
    {
        public Node rhs { get; }

        public NegationNode(Node rhs) 
        { 
            this.rhs = rhs;
        }

        internal override Node optimize()
        {
            Node rhs = this.rhs.optimize();
            if (rhs is NumberDoubleNode numberDoubleNode)
            {
                // If the operand is a number literal, negate it now
                // instead of waiting for evaluation.
                return new NumberDoubleNode(-numberDoubleNode.value);
            }
            else if (rhs is NumberIntNode numberIntNode)
            {
                // If the operand is a number literal, negate it now
                // instead of waiting for evaluation.
                return new NumberIntNode(-numberIntNode.value);
            }
            else if (rhs != this.rhs)
            {
                return new NegationNode(rhs);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            return "-" + this.rhs.ToString();
        }

        protected override bool EqualsSpecific(Node other)
        {
            NegationNode otherNode = (NegationNode)other;

            return this.rhs.Equals(otherNode.rhs);
        }
    }
}

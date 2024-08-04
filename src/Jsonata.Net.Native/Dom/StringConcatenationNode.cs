using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public sealed class StringConcatenationNode : Node
    {
        public Node lhs { get; }
        public Node rhs { get; }

        public StringConcatenationNode(Node lhs, Node rhs)
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
                return new StringConcatenationNode(lhs, rhs);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            return $"{this.lhs} & {this.rhs}";
        }

        protected override bool EqualsSpecific(Node other)
        {
            StringConcatenationNode otherNode = (StringConcatenationNode)other;
            return this.lhs.Equals(otherNode.lhs)
                && this.rhs.Equals(otherNode.rhs);
        }
    }
}

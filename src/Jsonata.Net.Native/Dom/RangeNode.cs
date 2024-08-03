using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    // A RangeNode represents the range operator.
    public sealed class RangeNode : Node
    {
        public Node lhs { get; }
        public Node rhs { get; }

        public RangeNode(Node lhs, Node rhs)
        {
            this.lhs = lhs ?? throw new ArgumentNullException(nameof(lhs));
            this.rhs = rhs ?? throw new ArgumentNullException(nameof(rhs));
        }

        internal override Node optimize()
        {
            Node rhs = this.rhs.optimize();
            Node lhs = this.lhs.optimize();
            if (lhs != this.lhs || rhs != this.rhs)
            {
                return new RangeNode(lhs, rhs);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            return this.lhs.ToString() + ".." + this.rhs.ToString();
        }
    }
}

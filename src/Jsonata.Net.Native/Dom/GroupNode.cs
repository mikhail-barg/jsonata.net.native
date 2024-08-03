using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    // A GroupNode represents a group expression.
    public sealed class GroupNode: Node
    {
        public Node expr { get; }
        public ObjectNode objectNode { get; }

        public GroupNode(Node expr, ObjectNode objectNode) 
        {
            this.expr = expr;
            this.objectNode = objectNode;
        }

        internal override Node optimize()
        {
            Node expr = this.expr.optimize();
            if (expr is GroupNode)
            {
                throw new JsonataException("S0210", "Each step can only have one grouping expression");
            };

            ObjectNode objectNode = (ObjectNode)this.objectNode.optimize();
            if (this.expr != expr || this.objectNode != objectNode)
            {
                return new GroupNode(expr, objectNode);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            return $"{this.expr}{this.objectNode}";
        }
    }
}

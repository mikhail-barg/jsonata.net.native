using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    // A ConditionalNode represents an if-then-else expression.
    public sealed class ConditionalNode : Node
    {
        public Node predicate { get; }
        public Node thenExpr { get; }
        public Node? elseExpr { get; }

        public ConditionalNode(Node predicate, Node thenExpr, Node? elseExpr)
        {
            this.predicate = predicate;
            this.thenExpr = thenExpr;
            this.elseExpr = elseExpr;
        }

        internal override Node optimize()
        {
            Node predicate = this.predicate.optimize();
            Node expr1 = this.thenExpr.optimize();
            Node? expr2 = this.elseExpr?.optimize();

            if (predicate != this.predicate
                || expr1 != this.thenExpr
                || expr2 != this.elseExpr
            )
            {
                return new ConditionalNode(predicate, expr1, expr2);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            if (this.elseExpr != null)
            {
                return $"{this.predicate} ? {this.thenExpr} : {this.elseExpr}";
            }
            else
            {
                return $"{this.predicate} ? {this.thenExpr}";
            }
        }
    }
}

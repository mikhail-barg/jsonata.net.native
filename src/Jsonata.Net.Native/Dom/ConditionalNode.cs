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
        public Node expr1 { get; }
        public Node? expr2 { get; }

        public ConditionalNode(Node predicate, Node expr1, Node? expr2)
        {
            this.predicate = predicate;
            this.expr1 = expr1;
            this.expr2 = expr2;
        }

        internal override Node optimize()
        {
            Node predicate = this.predicate.optimize();
            Node expr1 = this.expr1.optimize();
            Node? expr2 = this.expr2?.optimize();

            if (predicate != this.predicate
                || expr1 != this.expr1
                || expr2 != this.expr2
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
            if (this.expr2 != null)
            {
                return $"{this.predicate} ? {this.expr1} : {this.expr2}";
            }
            else
            {
                return $"{this.predicate} ? {this.expr1}";
            }
        }
    }
}

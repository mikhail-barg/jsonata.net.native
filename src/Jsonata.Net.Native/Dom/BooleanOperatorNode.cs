using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public sealed class BooleanOperatorNode: Node
    {
        public BooleanOperator op { get; }
        public Node lhs { get; }
        public Node rhs { get; }

        public enum BooleanOperator
        {
            BooleanAnd,
            BooleanOr,
        }

        public static string OperatorToString(BooleanOperator op) => op switch {
            BooleanOperator.BooleanAnd => "and",
            BooleanOperator.BooleanOr => "or",
            _ => throw new ArgumentException($"Unexpected operator '{op}'")
        };

        public BooleanOperatorNode(BooleanOperator op, Node lhs, Node rhs)
        {
            this.op = op;
            this.lhs = lhs;
            this.rhs = rhs;
        }

        internal override Node optimize()
        {
            Node lhs = this.lhs.optimize();
            Node rhs = this.rhs.optimize();

            if (lhs != this.lhs || rhs != this.rhs)
            {
                return new BooleanOperatorNode(this.op, lhs, rhs);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            return $"{this.lhs} {OperatorToString(this.op)} {this.rhs}";
        }
    }
}

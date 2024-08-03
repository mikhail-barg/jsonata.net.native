using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public sealed class ComparisonOperatorNode: Node
    {
        public enum ComparisonOperator
        {
            ComparisonEqual,
            ComparisonNotEqual,
            ComparisonLess,
            ComparisonLessEqual,
            ComparisonGreater,
            ComparisonGreaterEqual,
            ComparisonIn
        }

        public static string OperatorToString(ComparisonOperator op) => op switch {
            ComparisonOperator.ComparisonEqual => "=",
            ComparisonOperator.ComparisonNotEqual => "!=",
            ComparisonOperator.ComparisonLess => "<",
            ComparisonOperator.ComparisonLessEqual => "<=",
            ComparisonOperator.ComparisonGreater => ">",
            ComparisonOperator.ComparisonGreaterEqual => ">=",
            ComparisonOperator.ComparisonIn => "in",
            _ => throw new ArgumentException($"Unexpected operator '{op}'")
        };

        public ComparisonOperator op { get; }
        public Node lhs { get; }
        public Node rhs { get; }

        public ComparisonOperatorNode(ComparisonOperator op, Node lhs, Node rhs)
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
                return new ComparisonOperatorNode(this.op, lhs, rhs);
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

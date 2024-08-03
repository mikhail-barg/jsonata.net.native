using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public sealed class BooleanOperatorNode: Node
    {
        public Operator op { get; }
        public Node lhs { get; }
        public Node rhs { get; }

        public enum Operator
        {
            And,
            Or,
        }

        public static string OperatorToString(Operator op) => op switch {
            Operator.And => "and",
            Operator.Or => "or",
            _ => throw new ArgumentException($"Unexpected operator '{op}'")
        };

        public BooleanOperatorNode(Operator op, Node lhs, Node rhs)
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

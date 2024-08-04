using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public sealed class NumericOperatorNode : Node
    {
        public enum Operator
        {
            Add,
            Subtract,
            Multiply,
            Divide,
            Modulo
        }

        public static string OperatorToString(Operator op) => op switch {
            Operator.Add => "+",
            Operator.Subtract => "-",
            Operator.Multiply => "*",
            Operator.Divide => "/",
            Operator.Modulo => "%",
            _ => throw new ArgumentException($"Unexpected operator '{op}'")
        };

        public Operator op { get; }
        public Node lhs { get; }
        public Node rhs { get; }


        public NumericOperatorNode(Operator op, Node lhs, Node rhs)
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
                return new NumericOperatorNode(this.op, lhs, rhs);
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

        protected override bool EqualsSpecific(Node other)
        {
            NumericOperatorNode otherNode = (NumericOperatorNode)other;

            return this.op == otherNode.op
                && this.lhs.Equals(otherNode.lhs)
                && this.rhs.Equals(otherNode.rhs);
        }
    }
}

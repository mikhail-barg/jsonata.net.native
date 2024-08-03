using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public sealed class NumericOperatorNode : Node
    {
        public enum NumericOperator
        {
            NumericAdd,
            NumericSubtract,
            NumericMultiply,
            NumericDivide,
            NumericModulo
        }

        public static string OperatorToString(NumericOperator op) => op switch {
            NumericOperator.NumericAdd => "+",
            NumericOperator.NumericSubtract => "-",
            NumericOperator.NumericMultiply => "*",
            NumericOperator.NumericDivide => "/",
            NumericOperator.NumericModulo => "%",
            _ => throw new ArgumentException($"Unexpected operator '{op}'")
        };

        public NumericOperator op { get; }
        public Node lhs { get; }
        public Node rhs { get; }


        public NumericOperatorNode(NumericOperator op, Node lhs, Node rhs)
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
    }
}

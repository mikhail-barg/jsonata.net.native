using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Parsing;

namespace Jsonata.Net.Native.Dom
{
    // A BlockNode represents a block expression.
    public sealed class BlockNode : Node
    {
        private readonly List<Node> m_expressions;
        public IReadOnlyList<Node> expressions => this.m_expressions;

        public BlockNode(List<Node> expressions)
        {
            this.m_expressions = expressions ?? throw new ArgumentNullException(nameof(expressions));
        }

        internal override Node optimize()
        {
            for (int i = 0; i < this.m_expressions.Count; ++i)
            {
                this.m_expressions[i] = this.m_expressions[i].optimize();
            }
            return this;
        }

        public override string ToString()
        {
            return "(" + Helpers.JoinNodes(this.m_expressions, "; ") + ")";
        }
    }
}

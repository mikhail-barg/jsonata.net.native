using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public sealed class SortNode : Node
    {
        public enum Direction
        {
            Default,
            Ascending,
            Descending
        }

        public sealed class Term
        {
            public Direction dir { get; }
            public Node expr { get; }

            public Term(Direction dir, Node expr)
            {
                this.dir = dir;
                this.expr = expr;
            }
        }

        public Node expr { get; }
        public IReadOnlyList<Term> terms { get; }

        public SortNode(Node expr, IReadOnlyList<Term> terms)
        {
            this.expr = expr;
            this.terms = terms;
        }

        internal override Node optimize()
        {
            Node expr = this.expr.optimize();
            List<Term> terms = new List<Term>(this.terms.Count);
            foreach (Term term in this.terms)
            {
                Term newTerm = new Term(term.dir, term.expr.optimize());
                terms.Add(newTerm);
            }
            return new SortNode(expr, terms);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(this.expr.ToString());
            builder.Append("^(");
            foreach (Term term in this.terms)
            {
                switch (term.dir)
                {
                case Direction.Default:
                    break;
                case Direction.Ascending:
                    builder.Append("<");
                    break;
                case Direction.Descending:
                    builder.Append(">");
                    break;
                default:
                    throw new Exception("Unexpected direction " + term.dir);
                };
                builder.Append(term.expr.ToString());
            };
            builder.Append(")");
            return builder.ToString();
        }
    }
}

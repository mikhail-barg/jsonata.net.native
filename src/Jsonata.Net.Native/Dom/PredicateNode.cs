using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Parsing;

namespace Jsonata.Net.Native.Dom
{
    // A predicateNode is an interim data structure used when processing
    // predicate expressions. It is deliberately unexported and gets
    // converted into a PredicateNode during optimization.
    internal sealed class PredicateNode_ : Node
    {
        private readonly Node m_lhs;
        private readonly Node m_rhs;

        // lhs Node - the context for this predicate
        // rhs Node -  the predicate expression
        internal PredicateNode_(Node lhs, Node rhs)
        {
            this.m_lhs = lhs;
            this.m_rhs = rhs;
        }


        internal override Node optimize()
        {
            Node lhs = this.m_lhs.optimize();
            Node rhs = this.m_rhs.optimize();

            switch (lhs)
            {
            case GroupNode:
                throw new JsonataException("S0209", "A predicate cannot follow a grouping expression in a step"); //TODO: check if this is really not allowed
            case PathNode pathNode:
                {
                    Node last = pathNode.steps[pathNode.steps.Count - 1];
                    switch (last)
                    {
                    case PredicateNode predicateLastNode:
                        predicateLastNode.filters.Add(rhs);
                        break;
                    default:
                        pathNode.ReplaceLastStep(new PredicateNode(expr: last, filters: new List<Node>() { rhs }));
                        break;
                    }
                }
                return pathNode;
            default:
                return new PredicateNode(expr: lhs, filters: new List<Node>() { rhs });
            }
        }

        public override string ToString()
        {
            return $"{this.m_lhs}[{this.m_rhs}]";
        }

        protected override bool EqualsSpecific(Node other)
        {
            throw new NotImplementedException();
        }
    }

    // A PredicateNode represents a predicate expression.
    public sealed class PredicateNode : Node
    {
        public Node expr { get; }
        public List<Node> filters { get; }

        public PredicateNode(Node expr, List<Node> filters)
        {
            this.expr = expr;
            this.filters = filters;
        }

        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return $"{this.expr}[{Helpers.JoinNodes(this.filters, ", ")}]";
        }

        protected override bool EqualsSpecific(Node other)
        {
            PredicateNode otherNode = (PredicateNode)other;

            return this.expr.Equals(otherNode.expr)
                && Helpers.NodeListsEqual(this.filters, otherNode.filters);
        }
    }
}

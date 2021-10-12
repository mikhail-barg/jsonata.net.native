using System;
using System.Collections.Generic;
using System.Linq;

namespace Jsonata.Net.Native.Parsing
{
    public abstract record Node
    {
        internal abstract Node optimize();
    }

    internal sealed record StringNode(string value) : Node
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return this.value;
        }
    }

    internal sealed record NumberDoubleNode(double value) : Node
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return this.value.ToString();
        }
    }

    internal sealed record NumberIntNode(long value) : Node
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return this.value.ToString();
        }
    }


    internal sealed record BooleanNode(bool value) : Node
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return this.value.ToString();
        }
    }

    internal sealed record NullNode() : Node
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return "null";
        }
    }

    // A VariableNode represents a JSONata variable.
    internal sealed record VariableNode(string name) : Node
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return "$" + this.name;
        }
    }

    // A NameNode represents a JSON field name.
    internal sealed record NameNode(string value, bool escaped) : Node
    {
        internal override Node optimize()
        {
            return new PathNode(new List<Node>() { this }, keepArrays: false);
        }

        public override string ToString()
        {
            return this.escaped ?
                "`" + this.value + "`"
                : this.value;
        }
    }

    // A PathNode represents a JSON object path. It consists of one
    // or more 'steps' or Nodes (most commonly NameNode objects).
    internal sealed record PathNode(List<Node> steps, bool keepArrays) : Node
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            string result = Helpers.JoinNodes(this.steps, ".");
            if (this.keepArrays)
            {
                result += "[]";
            }
            return result;
        }
    }

    // A NegationNode represents a numeric negation operation.
    internal sealed record NegationNode(Node rhs) : Node
    {
        internal override Node optimize()
        {
            Node rhs = this.rhs.optimize();
            if (rhs is NumberDoubleNode numberDoubleNode)
            {
                // If the operand is a number literal, negate it now
                // instead of waiting for evaluation.
                return new NumberDoubleNode(-numberDoubleNode.value);
            }
            else if (rhs is NumberIntNode numberIntNode)
            {
                // If the operand is a number literal, negate it now
                // instead of waiting for evaluation.
                return new NumberIntNode(-numberIntNode.value);
            }
            else if (rhs != this.rhs)
            {
                return new NegationNode(rhs);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            return "-" + this.rhs.ToString();
        }
    }

    // A RangeNode represents the range operator.
    internal sealed record RangeNode(Node lhs, Node rhs) : Node
    {
        internal override Node optimize()
        {
            Node rhs = this.rhs.optimize();
            Node lhs = this.lhs.optimize();
            if (lhs != this.lhs || rhs != this.rhs)
            {
                return new RangeNode(lhs, rhs);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            return this.lhs.ToString() + ".." + this.rhs.ToString();
        }
    }

    // An ArrayNode represents an array of items.
    internal sealed record ArrayNode(List<Node> items) : Node
    {
        internal override Node optimize()
        {
            for (int i = 0; i < this.items.Count; ++i)
            {
                this.items[i] = this.items[i].optimize();
            }
            return this;
        }

        public override string ToString()
        {
            return "[" + Helpers.JoinNodes(this.items, ", ") + "]";
        }
    }

    // An ObjectNode represents an object, an unordered list of
    // key-value pairs.
    internal sealed record ObjectNode(List<Tuple<Node, Node>> pairs) : Node
    {
        internal override Node optimize()
        {
            for (int i = 0; i < this.pairs.Count; ++i)
            {
                this.pairs[i] = Tuple.Create(this.pairs[i].Item1.optimize(), this.pairs[i].Item2.optimize());
            }
            return this;
        }

        public override string ToString()
        {
            return "{" + String.Join(", ", this.pairs.Select(p => p.Item1.ToString() + ": " + p.Item2.ToString())) + "}";
        }
    }

    // A BlockNode represents a block expression.
    internal sealed record BlockNode(List<Node> expressions) : Node
    {
        internal override Node optimize()
        {
            for (int i = 0; i < this.expressions.Count; ++i)
            {
                this.expressions[i] = this.expressions[i].optimize();
            }
            return this;
        }

        public override string ToString()
        {
            return "(" + Helpers.JoinNodes(this.expressions, "; ") + ")";
        }
    }

    // A WildcardNode represents the wildcard operator.
    internal sealed record WildcardNode() : Node
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return "*";
        }
    }

    // A DescendentNode represents the descendent operator.
    internal sealed record DescendentNode() : Node
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return "**";
        }
    }

    // An ObjectTransformationNode represents the object transformation
    // operator.
    internal sealed record ObjectTransformationNode(Node pattern, Node updates, Node? deletes) : Node
    {
        internal override Node optimize()
        {
            Node pattern = this.pattern.optimize();
            Node updates = this.updates.optimize();
            Node? deletes = this.deletes?.optimize();
            if (pattern != this.pattern
                || updates != this.updates
                || deletes != this.deletes)
            {
                return new ObjectTransformationNode(pattern, updates, deletes);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            if (this.deletes != null)
            {
                return $"|{this.pattern}|{this.updates}, {this.deletes}|";
            }
            else
            {
                return $"|{this.pattern}|{this.updates}|";
            }
        }
    }

    // A dotNode is an interim structure used to process JSONata path
    // expressions. It is deliberately unexported and creates a PathNode
    // during its optimize phase.
    internal sealed record DotNode_(Node lhs, Node rhs) : Node
    {
        internal override Node optimize()
        {
            List<Node> steps = new List<Node>();
            bool keepArrays = false;

            //lhs
            {
                Node lhs = this.lhs.optimize();
                switch (lhs)
                {
                case NumberDoubleNode:
                case NumberIntNode:
                case BooleanNode:
                case NullNode:
                    throw new ErrPathLiteral(lhs.ToString());
                case StringNode stringNode:
                    //convert string to NameNode https://github.com/IBM/JSONata4Java/issues/25
                    steps.Add(new NameNode(stringNode.value, escaped: true));
                    break;
                case PathNode pathNode:
                    steps.AddRange(pathNode.steps);
                    keepArrays |= pathNode.keepArrays;
                    break;
                default:
                    steps.Add(lhs);
                    break;
                }
            }

            //rhs
            {
                Node rhs = this.rhs.optimize();
                switch (rhs)
                {
                case NumberDoubleNode:
                case NumberIntNode:
                case BooleanNode:
                case NullNode:
                    throw new ErrPathLiteral(rhs.ToString());
                case StringNode stringNode:
                    //convert string to NameNode https://github.com/IBM/JSONata4Java/issues/25
                    steps.Add(new NameNode(stringNode.value, escaped: true));
                    break;
                case PathNode pathNode:
                    steps.AddRange(pathNode.steps);
                    keepArrays |= pathNode.keepArrays;
                    break;
                default:
                    steps.Add(rhs);
                    break;
                }
            }

            return new PathNode(steps, keepArrays);
        }

        public override string ToString()
        {
            return $"{this.lhs}.{this.rhs}";
        }
    }

    // A predicateNode is an interim data structure used when processing
    // predicate expressions. It is deliberately unexported and gets
    // converted into a PredicateNode during optimization.
    internal sealed record PredicateNode_(Node lhs, Node rhs) : Node
    {
        // lhs Node - the context for this predicate
        // rhs Node -  the predicate expression

        internal override Node optimize()
        {
            Node lhs = this.lhs.optimize();
            Node rhs = this.rhs.optimize();

            switch (lhs)
            {
            case GroupNode:
                throw new ErrGroupPredicate();
            case PathNode pathNode:
                {
                    Node last = pathNode.steps[pathNode.steps.Count - 1];
                    switch (last)
                    {
                    case PredicateNode predicateLastNode:
                        predicateLastNode.filters.Add(rhs);
                        break;
                    default:
                        pathNode.steps.RemoveAt(pathNode.steps.Count - 1);
                        pathNode.steps.Add(new PredicateNode(expr: last, filters: new List<Node>() { rhs }));
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
            return $"{this.lhs}[{this.rhs}]";
        }
    }

    // A singletonArrayNode is an interim data structure used when
    // processing path expressions. It is deliberately unexported
    // and gets converted into a PathNode during optimization.
    internal sealed record SingletonArrayNode_(Node lhs) : Node
    {
        internal override Node optimize()
        {
            Node lhs = this.lhs.optimize();
            switch (lhs)
            {
            case PathNode pathNode:
                if (pathNode.keepArrays)
                {
                    return pathNode;
                }
                return new PathNode(pathNode.steps, keepArrays: true);
            default:
                return new PathNode(new List<Node>() { lhs }, keepArrays: true);
            }
        }

        public override string ToString()
        {
            return $"{this.lhs}[]";
        }
    }

    // A GroupNode represents a group expression.
    internal sealed record GroupNode(Node expr, ObjectNode objectNode) : Node
    {
        internal override Node optimize()
        {
            Node expr = this.expr.optimize();
            if (expr is GroupNode)
            {
                throw new ErrGroupGroup();
            };

            ObjectNode objectNode = (ObjectNode)this.objectNode.optimize();
            if (this.expr != expr || this.objectNode != objectNode)
            {
                return new GroupNode(expr, objectNode);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            return $"{this.expr}{this.objectNode}";
        }
    }

    // A PredicateNode represents a predicate expression.
    internal sealed record PredicateNode(Node expr, List<Node> filters) : Node
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return $"{this.expr}[{Helpers.JoinNodes(this.filters, ", ")}]";
        }
    }


    internal sealed record NumericOperatorNode(NumericOperatorNode.NumericOperator op, Node lhs, Node rhs) : Node
    {
        internal enum NumericOperator
        {
            NumericAdd,
            NumericSubtract,
            NumericMultiply,
            NumericDivide,
            NumericModulo
        }

        internal static string OperatorToString(NumericOperator op) => op switch {
            NumericOperator.NumericAdd => "+",
            NumericOperator.NumericSubtract => "+",
            NumericOperator.NumericMultiply => "+",
            NumericOperator.NumericDivide => "+",
            NumericOperator.NumericModulo => "+",
            _ => throw new ArgumentException($"Unexpected operator '{op}'")
        };

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
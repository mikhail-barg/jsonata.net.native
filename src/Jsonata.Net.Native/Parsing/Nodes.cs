using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

    internal abstract record NumberNode : Node
    {
        public abstract int GetIntValue();
    }

    internal sealed record NumberDoubleNode(double value) : NumberNode
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return this.value.ToString();
        }

        public override int GetIntValue()
        {
            return (int)this.value;
        }
    }

    internal sealed record NumberIntNode(long value) : NumberNode
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return this.value.ToString();
        }

        public override int GetIntValue()
        {
            return (int)this.value;
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
                    throw new JsonataException("S0213", $"The literal value {lhs} cannot be used as a step within a path expression");
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
                    throw new JsonataException("S0213", $"The literal value {rhs} cannot be used as a step within a path expression");
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
                throw new JsonataException("S0210", "Each step can only have one grouping expression");
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
            NumericOperator.NumericSubtract => "-",
            NumericOperator.NumericMultiply => "*",
            NumericOperator.NumericDivide => "/",
            NumericOperator.NumericModulo => "%",
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

    internal sealed record ComparisonOperatorNode(ComparisonOperatorNode.ComparisonOperator op, Node lhs, Node rhs) : Node
    {
        internal enum ComparisonOperator
        {
            ComparisonEqual,
            ComparisonNotEqual,
            ComparisonLess,
            ComparisonLessEqual,
            ComparisonGreater,
            ComparisonGreaterEqual,
            ComparisonIn
        }

        internal static string OperatorToString(ComparisonOperator op) => op switch {
            ComparisonOperator.ComparisonEqual => "=",
            ComparisonOperator.ComparisonNotEqual => "!=",
            ComparisonOperator.ComparisonLess => "<",
            ComparisonOperator.ComparisonLessEqual => "<=",
            ComparisonOperator.ComparisonGreater => ">",
            ComparisonOperator.ComparisonGreaterEqual => ">=",
            ComparisonOperator.ComparisonIn => "in",
            _ => throw new ArgumentException($"Unexpected operator '{op}'")
        };

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

    internal sealed record BooleanOperatorNode(BooleanOperatorNode.BooleanOperator op, Node lhs, Node rhs) : Node
    {
        internal enum BooleanOperator
        {
            BooleanAnd,
            BooleanOr,
        }

        internal static string OperatorToString(BooleanOperator op) => op switch {
            BooleanOperator.BooleanAnd => "and",
            BooleanOperator.BooleanOr => "or",
            _ => throw new ArgumentException($"Unexpected operator '{op}'")
        };

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

    internal sealed record StringConcatenationNode(Node lhs, Node rhs) : Node
    {
        internal override Node optimize()
        {
            Node lhs = this.lhs.optimize();
            Node rhs = this.rhs.optimize();

            if (lhs != this.lhs || rhs != this.rhs)
            {
                return new StringConcatenationNode(lhs, rhs);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            return $"{this.lhs} & {this.rhs}";
        }
    }

    // A FunctionCallNode represents a call to a function.
    internal sealed record FunctionCallNode(Node func, List<Node> args) : Node
    {
        internal override Node optimize()
        {
            Node func = this.func.optimize();
            List<Node> args = this.args.Select(a => a.optimize()).ToList();
            return new FunctionCallNode(func, args);
        }

        public override string ToString()
        {
            return $"{this.func}({this.args.JoinNodes(", ")})";
        }
    }


    // A LambdaNode represents a user-defined JSONata function.
    internal sealed record LambdaNode(bool isShorthand, List<string> paramNames, LambdaNode.Signature? signature, Node body) : Node
    {
        internal override Node optimize()
        {
            Node body = this.body.optimize();
            if (body != this.body)
            {
                return new LambdaNode(this.isShorthand, this.paramNames, this.signature, body);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(this.isShorthand ? "λ" : "function");
            builder.Append('(');
            for (int i = 0; i < this.paramNames.Count; ++i)
            {
                if (i != 0)
                {
                    builder.Append(",");
                };
                builder.Append('$');
                builder.Append(this.paramNames[i]);
            }
            builder.Append(')');
            if (this.signature != null)
            {
                this.signature.ToString(builder);
            };
            builder.Append('{');
            builder.Append(this.body.ToString());
            builder.Append('}');
            return builder.ToString();
        }

        internal enum ParamOpt
        {
            None,

            // ParamOptional denotes an optional parameter.
            Optional,

            // ParamVariadic denotes a variadic parameter.
            Variadic,

            // ParamContextable denotes a parameter that can be
            // replaced by the evaluation context if no value is
            // provided by the caller.
            Contextable
        };

        [Flags]
        internal enum ParamType
        {
            Bool = 0x01,
            Number = 0x02,
            String = 0x04,
            Null = 0x08,

            Array = 0x10,
            Object = 0x20,

            Func = 0x40,

            Simple = Bool | Number | String | Null,
            Json = Simple | Array | Object,
            Any = Json | Func,
            None = 0x0
        }

        internal static readonly Tuple<ParamType, char>[] s_paramTypePriorityLetters = {
            Tuple.Create(ParamType.Any, 'x'),
            Tuple.Create(ParamType.Json, 'j'),
            Tuple.Create(ParamType.Simple, 'u'),

            Tuple.Create(ParamType.Bool, 'b'),
            Tuple.Create(ParamType.Number, 'n'),
            Tuple.Create(ParamType.String, 's'),
            Tuple.Create(ParamType.Null, 'l'),

            Tuple.Create(ParamType.Array, 'a'),
            Tuple.Create(ParamType.Object, 'o'),

            Tuple.Create(ParamType.Func, 'f'),
        };

        internal sealed record Param(ParamType type, ParamOpt option, Signature? subSignature)
        {
            public static string ParamOptToString(ParamOpt opt)
            {
                switch (opt)
                {
                case ParamOpt.None:
                    return "";
                case ParamOpt.Optional:
                    return "?";
                case ParamOpt.Variadic:
                    return "+";
                case ParamOpt.Contextable:
                    return "-";
                default:
                    throw new Exception("Unexpected param opt " + opt);
                }
            }

            public static string ParamTypeToString(ParamType type)
            {
                foreach (Tuple<ParamType, char> t in s_paramTypePriorityLetters)
                {
                    if (type == t.Item1)
                    {
                        return t.Item2.ToString();
                    }
                }

                StringBuilder builder = new StringBuilder();
                builder.Append('(');
                foreach (Tuple<ParamType, char> t in s_paramTypePriorityLetters)
                {
                    if ((type & t.Item1) == t.Item1)
                    {
                        builder.Append(t.Item2);
                        type = type & ~t.Item1;
                    }
                }
                builder.Append(')');
                return builder.ToString();
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                this.ToString(builder);
                return builder.ToString();
            }

            internal void ToString(StringBuilder builder)
            {
                builder.Append(ParamTypeToString(this.type));
                if (this.subSignature != null)
                {
                    this.subSignature.ToString(builder);
                };
                builder.Append(ParamOptToString(this.option));
            }
        }

        internal sealed record Signature(List<Param> args, Param? result)
        {
            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                this.ToString(builder);
                return builder.ToString();
            }

            internal void ToString(StringBuilder builder)
            {
                builder.Append('<');
                foreach (Param arg in args)
                {
                    arg.ToString(builder);
                };
                if (result != null)
                {
                    builder.Append(':');
                    result.ToString(builder);
                };
                builder.Append('>');
            }
        }
    }

    // A PartialNode represents a partially applied function.
    internal sealed record PartialNode(Node func, List<Node> args) : Node
    {
        internal override Node optimize()
        {
            Node func = this.func.optimize();
            List<Node> args = this.args.Select(a => a.optimize()).ToList();
            return new PartialNode(func, args);
        }

        public override string ToString()
        {
            return $"{this.func}({this.args.JoinNodes(", ")})";
        }
    }

    // A PlaceholderNode represents a placeholder argument
    // in a partially applied function.
    internal sealed record PlaceholderNode() : Node
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return "?";
        }
    }

    // An AssignmentNode represents a variable assignment.
    internal sealed record AssignmentNode(string name, Node value) : Node
    {
        internal override Node optimize()
        {
            Node value = this.value.optimize();
            if (value != this.value)
            {
                return new AssignmentNode(this.name, value);
            }
            else
            {
                return this;
            }
        }
    }

    // A FunctionApplicationNode represents a function application
    // operation.
    internal sealed record FunctionApplicationNode(Node lhs, Node rhs) : Node
    {
        internal override Node optimize()
        {
            Node lhs = this.lhs.optimize();
            Node rhs = this.rhs.optimize();
            if (lhs !=this.lhs || rhs != this.rhs)
            {
                return new FunctionApplicationNode(lhs, rhs);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            return $"{this.lhs} ~> {this.rhs}";
        }
    }


    // A ConditionalNode represents an if-then-else expression.
    internal sealed record ConditionalNode(Node predicate, Node expr1, Node? expr2): Node
    {
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

    
    internal sealed record SortNode(Node expr, List<SortNode.Term> terms): Node
    {

        internal enum Direction
        {
            Default,
            Ascending,
            Descending
        }

        internal sealed record Term(Direction dir, Node expr)
        {
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

    internal sealed record RegexNode(System.Text.RegularExpressions.Regex regex, string pattern) : Node
    {
        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return "/" + this.pattern + "/";
        }
    }
}
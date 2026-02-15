using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Jsonata.Net.Native.Impl
{
    public enum SymbolType
    {
        path,
        variable,
        sort,
        binary,
        @operator,
        regex,
        @string,
        //number, //see _number_* instead
        name,
        //value, // see _value_* instead
        //unary, //see _unary_* instead
        function,
        lambda,
        condition,
        block,
        wildcard,
        parent, //also see _parent
        filter,
        bind,
        apply,
        partial,
        transform,
        descendant,
        //error,
        index,

        //added in dotnet to make proper handling
        _end, 
        _sort_term,
        _slot,
        _binary_orderby,            //was a part of 'binary', gets optimized away during processAST() phase
        _binary_groupby,            //was a part of 'binary', gets optimized away during processAST() phase
        _binary_bind_context,       //was a part of 'binary', gets optimized away during processAST() phase
        _binary_bind_positional,    //was a part of 'binary', gets optimized away during processAST() phase
        _binary_bind_assign,        //was a part of 'binary', gets optimized away during processAST() phase //TODO: converge with 'bind'?
        _binary_path_node,          //was a part of 'binary', gets optimized away during processAST() phase //TODO: converge with 'path'?
        _binary_filter_node,        //was a part of 'binary', gets optimized away during processAST() phase 
        _unary_group,       //was a part of 'unary'
        _unary_minus,       //was a part of 'unary'
        _unary_array,       //was a part of 'unary'
        _number_int,        //was a part of 'number'
        _number_double,     //was a part of 'number'
        _value_bool,        //was a part of 'value
        _value_null,        //was a part of 'value
        _parent,            // gets converted to 'parent' during processAST() phase
        _partial_arg,       //was a part of @operator
    }

    public enum BinaryOperatorType
    {
        and,    // and
        or,     // or
        add,    // +
        sub,    // -
        mul,    // *
        div,    // /
        mod,    // %
        eq,     // =
        ne,     // !=
        lt,     // <
        le,     // <=
        gt,     // >
        ge,     // >=
        concat, // &
        range,  // ..
        @in,    // in
    }

    public enum SpecialOperatorType
    {
        and,
        or,
        @in
    }

    public abstract class Node: IEquatable<Node>
    {
        public readonly SymbolType type;
        public readonly int position;

        public bool tuple { get; internal set; } = false;
        public bool consarray { get; internal set; } = false;
        public bool keepArray { get; internal set; }
        public GroupNode? group { get; internal set; }
        public List<StageRuntimeNode>? stages { get; internal set; }
        public List<FilterRuntimeNode>? predicate { get; internal set; }

        // Ancestor attributes
        internal SlotRuntimeNode? ancestor;
        internal List<SlotRuntimeNode>? seekingParent;
        internal string? index; // positional binding
        internal string? focus; // contextual binding

        protected Node(SymbolType type, int position)
        {
            this.type = type;
            this.position = position;
        }

        public override string ToString()
        {
            return $"{this.GetType().Name} {this.type}";
        }

        //used for DOM comparison only

        protected static bool IsNullMatches(object? a, object? b)
        {
            return (a == null) == (b == null);
        }

        // from Object — just to be constent:
        // when implementing IEquatable -> override default Object.Equals
        public override bool Equals(object? obj) 
        {
            return this.EqualsImpl(obj as Node);
        }

        // actually it should not be used,
        // but considering we are overriding Object.Equals, we should also make changes to GetHashCode
        public override int GetHashCode()   
        {
            // I'm not willing to spend time implementing proper GetHasCode for each node type,
            // because it is not intended to be used as Dictionary key
            // but this is a formally consistent (while very bad) implementation.
            return 0;
        }

        public bool Equals(Node? other) // from IEquatable
        {
            return this.EqualsImpl(other);
        }

        // actual implementation
        // all the descendant classes would have to implement IEquatable<Decendant>
        // because EqualityComparer<Descendant>.Default will not consider base Node implementing IEquatable<Node>
        // and EqualityComparer.Default is being used for example by Enumerable.SequenceEqual
        protected bool EqualsImpl(Node? other) 
        {
            if (other == null)
            {
                return false;
            }

            if (other.GetType() != this.GetType())
            {
                return false;
            }

            if (other.type != this.type
                || other.tuple != this.tuple
                || other.consarray != this.consarray
                || other.keepArray != this.keepArray
            )
            {
                return false;
            }

            if (!IsNullMatches(this.group, other.group))
            {
                return false;
            }
            if (this.group != null && other.group != null
                && !this.group.Equals(other.group)
            )
            {
                return false;
            }

            if (!IsNullMatches(this.stages, other.stages))
            {
                return false;
            }

            if (this.stages != null && other.stages != null
                && !Enumerable.SequenceEqual(this.stages, other.stages) //uses Equals
            )
            {
                return false;
            }

            if (!IsNullMatches(this.predicate, other.predicate))
            {
                return false;
            }

            if (this.predicate != null && other.predicate != null
                && !Enumerable.SequenceEqual(this.predicate, other.predicate) //uses Equals
            )
            {
                return false;
            }

            // TODO: should check ancestor attributes??

            if (!this.EqualsSpecific(other))
            {
                return false;
            }

            return true;
        }

        protected abstract bool EqualsSpecific(Node other);

        public string PrintAst()
        {
            StringBuilder sb = new StringBuilder();
            this.PrintAstInternal(sb, 0);
            return sb.ToString();
        }

        protected internal void PrintAstInternal(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent);
            sb.Append(this.GetType().Name).Append("(").Append(this.type.ToString()).Append(")");
            this.PrintAstSpecific(sb);
            if (this.tuple)
            {
                sb.Append(", tuple");
            }
            if (this.consarray)
            {
                sb.Append(", consarray");
            }
            if (this.keepArray)
            {
                sb.Append(", keepArray");
            }
            sb.Append('\n');
            this.PrintAstChildren(sb, indent + 1);
            if (this.group != null)
            {
                this.PrintIndent(sb, indent + 1).Append("group:\n");
                this.group.PrintAstInternal(sb, indent + 2);
            }
            if (this.stages != null)
            {
                this.PrintIndent(sb, indent + 1).Append("stages[").Append(this.stages.Count).Append("]\n");
                foreach (StageRuntimeNode stage in this.stages)
                {
                    stage.PrintAstInternal(sb, indent + 2);
                }
            }
            if (this.predicate != null)
            {
                this.PrintIndent(sb, indent + 1).Append("predicate[").Append(this.predicate.Count).Append("]\n");
                foreach (FilterRuntimeNode stage in this.predicate)
                {
                    stage.PrintAstInternal(sb, indent + 2);
                }
            }
        }

        protected StringBuilder PrintIndent(StringBuilder sb, int indent)
        {
            for (int i = 0; i < indent; ++i)
            {
                sb.Append('\t');
            }
            return sb;
        }

        protected abstract void PrintAstSpecific(StringBuilder sb);
        protected abstract void PrintAstChildren(StringBuilder sb, int indent);
    }

    public sealed class EndNode: Node, IEquatable<EndNode>
    {
        public EndNode(int position)
            : base(SymbolType._end, position)
        {
        }

        public bool Equals(EndNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            throw new NotImplementedException();
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            throw new NotImplementedException();
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class FilterConstructionNode: Node, IEquatable<FilterConstructionNode>
    {
        public readonly Node lhs;
        public readonly Node rhs;

        public FilterConstructionNode(Node lhs, Node rhs, int position = -1)
            : base(SymbolType._binary_filter_node, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        public bool Equals(FilterConstructionNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            FilterConstructionNode otherBinary = (FilterConstructionNode)other;
            if (!this.lhs.Equals(otherBinary.lhs))
            {
                return false;
            }
            if (!this.rhs.Equals(otherBinary.rhs))
            {
                return false;
            }
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("lhs\n");
            this.lhs.PrintAstInternal(sb, indent + 1);
            this.PrintIndent(sb, indent).Append("rhs\n");
            this.rhs.PrintAstInternal(sb, indent + 1);
        }
    }

    public sealed class PathConstructionNode: Node, IEquatable<PathConstructionNode>
    {
        public readonly Node lhs;
        public readonly Node rhs;

        public PathConstructionNode(Node lhs, Node rhs, int position = -1)
            : base(SymbolType._binary_path_node, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        public bool Equals(PathConstructionNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            PathConstructionNode otherBinary = (PathConstructionNode)other;
            if (!this.lhs.Equals(otherBinary.lhs))
            {
                return false;
            }
            if (!this.rhs.Equals(otherBinary.rhs))
            {
                return false;
            }
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("lhs\n");
            this.lhs.PrintAstInternal(sb, indent + 1);
            this.PrintIndent(sb, indent).Append("rhs\n");
            this.rhs.PrintAstInternal(sb, indent + 1);
        }

    }

    public sealed class DescendantNode: Node, IEquatable<DescendantNode>
    {
        public DescendantNode(int position = -1)
            : base(SymbolType.descendant, position)
        {
        }

        public bool Equals(DescendantNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            //nothing to do
        }
    }

    public sealed class ParentConstructionNode: Node, IEquatable<ParentConstructionNode>
    {
        public ParentConstructionNode(int position = -1)
            : base(SymbolType._parent, position)
        {
        }

        public bool Equals(ParentConstructionNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            //nothing to do
        }
    }

    public sealed class ParentOptimizedNode: Node, IEquatable<ParentOptimizedNode>
    {
        public readonly SlotRuntimeNode slot;
        public ParentOptimizedNode(SlotRuntimeNode slot, int position = -1)
            : base(SymbolType.parent, position)
        {
            this.slot = slot;
        }

        public bool Equals(ParentOptimizedNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            ParentOptimizedNode otherParent = (ParentOptimizedNode)other;
            return this.slot.Equals(otherParent.slot);
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("slot\n");
            this.slot.PrintAstInternal(sb, indent + 1);
        }
    }

    public sealed class WildcardNode: Node, IEquatable<WildcardNode>
    {
        public WildcardNode(int position = -1)
            : base(SymbolType.wildcard, position)
        {
        }

        public bool Equals(WildcardNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            //nothing to do
        }
    }

    public sealed class RegexNode: Node, IEquatable<RegexNode>
    {
        public readonly Regex regex;

        public RegexNode(Regex regex, int position = -1)
            : base(SymbolType.regex, position)
        {
            this.regex = regex;
        }

        public bool Equals(RegexNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            RegexNode otherRegex = (RegexNode)other;
            return this.regex.ToString().Equals(otherRegex.regex.ToString());
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            sb.Append(" `").Append(this.regex.ToString()).Append("`");
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            //nothing to do
        }
    }

    public abstract class NodeWithStrValue : Node, IEquatable<NodeWithStrValue>
    {
        public readonly string value;

        public NodeWithStrValue(SymbolType type, string value, int position)
            : base(type, position)
        {
            this.value = value;
        }

        public bool Equals(NodeWithStrValue? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            NodeWithStrValue otherWithValue = (NodeWithStrValue)other;
            return this.value.Equals(otherWithValue.value);
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            sb.Append(" `").Append(this.value).Append("`");
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            //nothing to do
        }
    }


    public sealed class NameNode: NodeWithStrValue, IEquatable<NameNode>
    {
        public NameNode(string value, int position = -1)
            : base(SymbolType.name, value, position)
        {
        }

        public bool Equals(NameNode? other)
        {
            return this.EqualsImpl(other);
        }
    }

    public sealed class VariableNode: NodeWithStrValue, IEquatable<VariableNode>
    {
        public VariableNode(string value, int position = -1)
            : base(SymbolType.variable, value, position)
        {
        }

        public bool Equals(VariableNode? other)
        {
            return this.EqualsImpl(other);
        }
    }

    public sealed class AssignVarConstructionNode: Node, IEquatable<AssignVarConstructionNode>
    {
        public readonly VariableNode lhs;
        public readonly Node rhs;

        public AssignVarConstructionNode(VariableNode lhs, Node rhs, int position = -1)
            : base(SymbolType._binary_bind_assign, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        // convenience for manual construction
        public AssignVarConstructionNode(string variable, Node rhs)
            :this(new VariableNode(variable), rhs)
        {

        }


        public bool Equals(AssignVarConstructionNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            AssignVarConstructionNode otherBind = (AssignVarConstructionNode)other;
            if (!this.lhs.Equals(otherBind.lhs))
            {
                return false;
            }
            if (!this.rhs.Equals(otherBind.rhs))
            {
                return false;
            }
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("lhs\n");
            this.lhs.PrintAstInternal(sb, indent + 1);
            this.PrintIndent(sb, indent).Append("rhs\n");
            this.rhs.PrintAstInternal(sb, indent + 1);
        }
    }

    public sealed class BindPositionalVarConstructionNode: Node, IEquatable<BindPositionalVarConstructionNode>
    {
        public readonly Node lhs;
        public readonly VariableNode rhs;

        public BindPositionalVarConstructionNode(Node lhs, VariableNode rhs, int position = -1)
            : base(SymbolType._binary_bind_positional, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        public BindPositionalVarConstructionNode(Node lhs, string variable)
            :this(lhs, new VariableNode(variable))
        {

        }

        public bool Equals(BindPositionalVarConstructionNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            BindPositionalVarConstructionNode otherBind = (BindPositionalVarConstructionNode)other;
            if (!this.lhs.Equals(otherBind.lhs))
            {
                return false;
            }
            if (!this.rhs.Equals(otherBind.rhs))
            {
                return false;
            }
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("lhs\n");
            this.lhs.PrintAstInternal(sb, indent + 1);
            this.PrintIndent(sb, indent).Append("rhs\n");
            this.rhs.PrintAstInternal(sb, indent + 1);
        }
    }

    public sealed class BindContextVarConstructionNode: Node, IEquatable<BindContextVarConstructionNode>
    {
        public readonly Node lhs;
        public readonly VariableNode rhs;

        public BindContextVarConstructionNode(Node lhs, VariableNode rhs, int position = -1)
            : base(SymbolType._binary_bind_context, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        public BindContextVarConstructionNode(Node lhs, string variable)
            : this(lhs, new VariableNode(variable))
        {

        }

        public bool Equals(BindContextVarConstructionNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            BindContextVarConstructionNode otherBind = (BindContextVarConstructionNode)other;
            if (!this.lhs.Equals(otherBind.lhs))
            {
                return false;
            }
            if (!this.rhs.Equals(otherBind.rhs))
            {
                return false;
            }
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("lhs\n");
            this.lhs.PrintAstInternal(sb, indent + 1);
            this.PrintIndent(sb, indent).Append("rhs\n");
            this.rhs.PrintAstInternal(sb, indent + 1);
        }
    }

    public sealed class ArgumentPlaceholderNode: Node, IEquatable<ArgumentPlaceholderNode>
    {
        public ArgumentPlaceholderNode(int position = -1)
            : base(SymbolType._partial_arg, position)
        {
        }

        public bool Equals(ArgumentPlaceholderNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            sb.Append(" `?`");
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            //nothing to do
        }
    }

    public sealed class SpecialOperatorConstructionNode: Node, IEquatable<SpecialOperatorConstructionNode>
    {
        public readonly SpecialOperatorType value;

        public SpecialOperatorConstructionNode(SpecialOperatorType value, int position = -1)
            : base(SymbolType.@operator, position)
        {
            this.value = value;
        }

        public bool Equals(SpecialOperatorConstructionNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            SpecialOperatorConstructionNode otherOperator = (SpecialOperatorConstructionNode)other;
            return this.value.ToString().Equals(otherOperator.value.ToString());
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            sb.Append(" `").Append(this.value.ToString()).Append("`");
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            //nothing to do
        }
    }

    public sealed class StringNode: Node, IEquatable<StringNode>
    {
        public readonly string value;

        public StringNode(string value, int position = -1)
            : base(SymbolType.@string, position)
        {
            this.value = value;
        }

        public bool Equals(StringNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            StringNode otherString = (StringNode)other;
            return this.value.ToString().Equals(otherString.value.ToString());
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            sb.Append(" `").Append(this.value).Append("`");
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            //nothing to do
        }
    }

    public sealed class NullNode: Node, IEquatable<NullNode>
    {
        public NullNode(int position = -1)
            : base(SymbolType._value_null, position)
        {
        }

        public bool Equals(NullNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            return true;
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            //nothing to do
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }
    }

    public sealed class BoolNode: Node, IEquatable<BoolNode>
    {
        public readonly bool value;

        public BoolNode(bool value, int position = -1)
            : base(SymbolType._value_bool, position)
        {
            this.value = value;
        }

        public bool Equals(BoolNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            BoolNode otherBool = (BoolNode)other;
            return this.value.ToString().Equals(otherBool.value.ToString());
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            sb.Append(" `").Append(this.value).Append("`");
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            //nothing to do
        }
    }

    public sealed class NumberIntNode: Node, IEquatable<NumberIntNode>
    {
        public readonly long value;

        public NumberIntNode(long value, int position = -1)
            :base(SymbolType._number_int, position)
        {
            this.value = value;
        }

        public bool Equals(NumberIntNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            NumberIntNode otherInt = (NumberIntNode)other;
            return this.value.ToString().Equals(otherInt.value.ToString());
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            sb.Append(" `").Append(this.value).Append("`");
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            //nothing to do
        }
    }

    public sealed class NumberDoubleNode: Node, IEquatable<NumberDoubleNode>
    {
        public readonly double value;

        public NumberDoubleNode(double value, int position = -1)
            : base(SymbolType._number_double, position)
        {
            this.value = value;
        }

        public bool Equals(NumberDoubleNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            NumberDoubleNode otherDouble = (NumberDoubleNode)other;
            return this.value.ToString().Equals(otherDouble.value.ToString());
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            sb.Append(" `").Append(this.value).Append("`");
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            //nothing to do
        }
    }
    public sealed class LambdaNode: Node, IEquatable<LambdaNode>
    {
        public readonly List<VariableNode> arguments;
        public readonly Signature? signature;
        public readonly Node body;
        public readonly bool thunk;

        public LambdaNode(List<VariableNode> arguments, Node body, Signature? signature = null, bool thunk = false, int position = -1)
            :base(SymbolType.lambda, position)
        {
            this.arguments = arguments;
            this.signature = signature;
            this.body = body;
            this.thunk = thunk;
        }

        public bool Equals(LambdaNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            LambdaNode otherLambda = (LambdaNode)other;
            if (this.thunk != otherLambda.thunk)
            {
                return false;
            }
            if (!this.body.Equals(otherLambda.body))
            {
                return false;
            }
            if (!IsNullMatches(this.signature, otherLambda.signature))
            {
                return false;
            }
            if (this.signature != null && otherLambda.signature != null
                && !this.signature.Equals(otherLambda.signature)
            )
            {
                return false;
            }

            if (!Enumerable.SequenceEqual(this.arguments, otherLambda.arguments))
            {
                return false;
            }

            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            if (this.signature != null)
            {
                sb.Append(" ").Append(this.signature);
            }
            if (this.thunk)
            {
                sb.Append(", thunk");
            }
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("body:\n");
            this.body.PrintAstInternal(sb, indent + 1);
            this.PrintIndent(sb, indent).Append("args[").Append(this.arguments.Count).Append("]:\n");
            foreach (VariableNode arg in this.arguments)
            {
                arg.PrintAstInternal(sb, indent + 1);
            }
        }
    }

    public sealed class FunctionalNode: Node, IEquatable<FunctionalNode>
    {
        public readonly bool partial;
        public readonly Node procedure;
        public readonly List<Node> arguments;

        public FunctionalNode(Node procedure, List<Node> arguments, bool partial = false, int position = -1)
            :base(partial? SymbolType.partial : SymbolType.function, position)
        {
            this.partial = partial;
            this.procedure = procedure;
            this.arguments = arguments;
        }

        public FunctionalNode(string function, List<Node> arguments)
            :this(new VariableNode(function), arguments)
        {

        }

        public bool Equals(FunctionalNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            FunctionalNode otherFunctional = (FunctionalNode)other;
            if (!this.procedure.Equals(otherFunctional.procedure))
            {
                return false;
            }
            if (!Enumerable.SequenceEqual(this.arguments, otherFunctional.arguments))
            {
                return false;
            }
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            if (this.partial)
            {
                sb.Append(", `partial`");
            }
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("procedure:\n");
            this.procedure.PrintAstInternal(sb, indent + 1);
            this.PrintIndent(sb, indent).Append("args[").Append(this.arguments.Count).Append("]:\n");
            foreach (Node arg in this.arguments)
            {
                arg.PrintAstInternal(sb, indent + 1);
            }
        }

    }

    public sealed class SlotRuntimeNode: Node, IEquatable<SlotRuntimeNode>
    {
        public string label;
        public int level;
        public readonly int ancestorIndex;

        public SlotRuntimeNode(string label, int ancestorIndex, int level)
            : base(SymbolType._slot, -1)
        {
            this.label = label;
            this.ancestorIndex = ancestorIndex;
            this.level = level;
        }

        public bool Equals(SlotRuntimeNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            SlotRuntimeNode otherSlot = (SlotRuntimeNode)other;
            return (this.label == otherSlot.label
                && this.level == otherSlot.level
                && this.ancestorIndex == otherSlot.ancestorIndex
            );
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            sb.Append(" `").Append(this.label).Append("`, ").Append(this.level).Append(" ").Append(this.index);
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            //nothing to do
        }
    }

    public sealed class UnaryMinusNode: Node, IEquatable<UnaryMinusNode>
    {
        public readonly Node expression;

        public UnaryMinusNode(Node expression, int position = -1)
            : base(SymbolType._unary_minus, position)
        {
            this.expression = expression;
        }

        public bool Equals(UnaryMinusNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            UnaryMinusNode otherMinus = (UnaryMinusNode)other;
            return this.expression.Equals(otherMinus.expression);
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.expression.PrintAstInternal(sb, indent);
        }
    }

    public sealed class SortTermNode: Node, IEquatable<SortTermNode>
    {
        public readonly Node expression;
        public readonly bool descending;

        public SortTermNode(Node expression, bool descending, int position = -1)
            :base(SymbolType._sort_term, position)
        {
            this.expression = expression;
            this.descending = descending;
        }

        public bool Equals(SortTermNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            SortTermNode otherTerm = (SortTermNode)other;

            return this.descending == otherTerm.descending
                && this.expression.Equals(otherTerm.expression);
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            if (!this.descending)
            {
                sb.Append(" asc");
            }
            else
            {
                sb.Append(" desc");
            }
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.expression.PrintAstInternal(sb, indent);
        }
    }

    public sealed class SortStepRuntimeNode: Node, IEquatable<SortStepRuntimeNode>
    {
        public readonly List<SortTermNode> terms;

        public SortStepRuntimeNode(List<SortTermNode> terms, int position = -1)
            :base(SymbolType.sort, position) 
        { 
            this.terms = terms;
        }

        public bool Equals(SortStepRuntimeNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            SortStepRuntimeNode otherSort = (SortStepRuntimeNode)other;
            return Enumerable.SequenceEqual(this.terms, otherSort.terms);
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("terms[").Append(this.terms.Count).Append("]:\n");
            foreach (SortTermNode arg in this.terms)
            {
                arg.PrintAstInternal(sb, indent + 1);
            }
        }
    }

    public abstract class StageRuntimeNode: Node, IEquatable<StageRuntimeNode>
    {
        internal StageRuntimeNode(SymbolType type, int position) 
            : base(type, position)
        {
        }

        public bool Equals(StageRuntimeNode? other)
        {
            return this.EqualsImpl(other);
        }
    }

    public sealed class FilterRuntimeNode: StageRuntimeNode, IEquatable<FilterRuntimeNode>
    {
        public readonly Node expr;

        public FilterRuntimeNode(Node expr, int position = -1)
            :base(SymbolType.filter, position)
        {
            this.expr = expr;
        }

        public bool Equals(FilterRuntimeNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            FilterRuntimeNode otherFilt = (FilterRuntimeNode)other;
            return this.expr.Equals(otherFilt.expr);
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.expr.PrintAstInternal(sb, indent);
        }
    }

    public sealed class IndexRuntimeNode: StageRuntimeNode, IEquatable<IndexRuntimeNode>
    {
        public readonly string indexValue;

        public IndexRuntimeNode(string indexValue, int position = -1)
            : base(SymbolType.index, position)
        {
            this.indexValue = indexValue;
        }

        public bool Equals(IndexRuntimeNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            IndexRuntimeNode otherIndex = (IndexRuntimeNode)other;
            return this.indexValue == otherIndex.indexValue;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            sb.Append(" `").Append(this.indexValue).Append("`");
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            //nothing to do
        }
    }

    public sealed class BlockNode: Node, IEquatable<BlockNode>
    {
        public readonly List<Node> expressions;

        public BlockNode(List<Node> expressions, int position = -1)
            : base(SymbolType.block, position)
        {
            this.expressions = expressions;
        }

        public bool Equals(BlockNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            BlockNode otherBlock = (BlockNode)other;
            return Enumerable.SequenceEqual(this.expressions, otherBlock.expressions);
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("expressions[").Append(this.expressions.Count).Append("]:\n");
            foreach (Node arg in this.expressions)
            {
                arg.PrintAstInternal(sb, indent + 1);
            }
        }
    }

    public sealed class ArrayNode: Node, IEquatable<ArrayNode>
    {
        public readonly List<Node> expressions;

        public ArrayNode(List<Node> expressions, int position = -1)
            : base(SymbolType._unary_array, position)
        {
            this.expressions = expressions;
        }

        public bool Equals(ArrayNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            ArrayNode otherArray = (ArrayNode)other;
            return Enumerable.SequenceEqual(this.expressions, otherArray.expressions);
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("expressions[").Append(this.expressions.Count).Append("]:\n");
            foreach (Node arg in this.expressions)
            {
                arg.PrintAstInternal(sb, indent + 1);
            }
        }
    }

    public sealed class ApplyNode: Node, IEquatable<ApplyNode>
    {
        public readonly Node lhs;
        public readonly Node rhs;

        public ApplyNode(Node lhs, Node rhs, int position = -1)
            : base(SymbolType.apply, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        public bool Equals(ApplyNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            ApplyNode otherApply = (ApplyNode)other;
            if (!this.lhs.Equals(otherApply.lhs))
            {
                return false;
            }
            if (!this.rhs.Equals(otherApply.rhs))
            {
                return false;
            }
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("lhs\n");
            this.lhs.PrintAstInternal(sb, indent + 1);
            this.PrintIndent(sb, indent).Append("rhs\n");
            this.rhs.PrintAstInternal(sb, indent + 1);
        }
    }

    public sealed class BindRuntimeNode: Node, IEquatable<BindRuntimeNode>
    {
        public readonly VariableNode lhs;
        public readonly Node rhs;

        public BindRuntimeNode(VariableNode lhs, Node rhs, int position = -1)
            : base(SymbolType.bind, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        public bool Equals(BindRuntimeNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            BindRuntimeNode otherBind = (BindRuntimeNode)other;
            if (!this.lhs.Equals(otherBind.lhs))
            {
                return false;
            }
            if (!this.rhs.Equals(otherBind.rhs))
            {
                return false;
            }
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("lhs\n");
            this.lhs.PrintAstInternal(sb, indent + 1);
            this.PrintIndent(sb, indent).Append("rhs\n");
            this.rhs.PrintAstInternal(sb, indent + 1);
        }
    }

    public sealed class GroupNode: Node, IEquatable<GroupNode>
    {
        public readonly List<Tuple<Node, Node>> lhsObject;

        public GroupNode(List<Tuple<Node, Node>> lhsObject, int position = -1)
            : base(SymbolType._unary_group, position)
        {
            this.lhsObject = lhsObject;
        }

        public bool Equals(GroupNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            GroupNode otherGroup = (GroupNode)other;
            return Enumerable.SequenceEqual(this.lhsObject, otherGroup.lhsObject);
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("lhsObject[").Append(this.lhsObject.Count).Append("*2]:\n");
            for (int i = 0; i < this.lhsObject.Count; ++i)
            {
                this.PrintIndent(sb, indent + 1).Append(i).Append(":\n");
                Tuple<Node, Node> arg = this.lhsObject[i];
                arg.Item1.PrintAstInternal(sb, indent + 1);
                arg.Item2.PrintAstInternal(sb, indent + 1);
            }
        }
    }

    public sealed class GroupByConstructionNode: Node, IEquatable<GroupByConstructionNode>
    {
        public readonly Node lhs;
        public readonly List<Tuple<Node, Node>> rhsObject;

        public GroupByConstructionNode(Node lhs, List<Tuple<Node, Node>> rhsObject, int position = -1)
            : base(SymbolType._binary_groupby, position)
        {
            this.lhs = lhs;
            this.rhsObject = rhsObject;
        }

        public bool Equals(GroupByConstructionNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            GroupByConstructionNode otherGroup = (GroupByConstructionNode)other;
            return this.lhs.Equals(otherGroup.lhs) 
                && Enumerable.SequenceEqual(this.rhsObject, otherGroup.rhsObject);
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("lhs:\n");
            this.lhs.PrintAstInternal(sb, indent + 1);
            this.PrintIndent(sb, indent).Append("rhsObject[").Append(this.rhsObject.Count).Append("*2]:\n");
            for (int i = 0; i < this.rhsObject.Count; ++i)
            {
                this.PrintIndent(sb, indent + 1).Append(i).Append(":\n");
                Tuple<Node, Node> arg = this.rhsObject[i];
                arg.Item1.PrintAstInternal(sb, indent + 1);
                arg.Item2.PrintAstInternal(sb, indent + 1);
            }
        }
    }

    public sealed class BinaryNode: Node, IEquatable<BinaryNode>
    {
        public readonly Node lhs;
        public readonly Node rhs;
        public readonly BinaryOperatorType value;

        public BinaryNode(BinaryOperatorType value, Node lhs, Node rhs, int position = -1)
            :base(SymbolType.binary, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
            this.value = value;
        }

        public bool Equals(BinaryNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            BinaryNode otherBinary = (BinaryNode)other;
            if (this.value != otherBinary.value)
            {
                return false;
            }
            if (!this.lhs.Equals(otherBinary.lhs))
            {
                return false;
            }
            if (!this.rhs.Equals(otherBinary.rhs))
            {
                return false;
            }
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            sb.Append(" ").Append(this.value.ToString());
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("lhs\n");
            this.lhs.PrintAstInternal(sb, indent + 1);
            this.PrintIndent(sb, indent).Append("rhs\n");
            this.rhs.PrintAstInternal(sb, indent + 1);
        }
    }


    public sealed class PathRuntimeNode: Node, IEquatable<PathRuntimeNode>
    {
        public readonly List<Node> steps;
        public bool keepSingletonArray { get; internal set; } = false;

        public PathRuntimeNode(List<Node> steps)
            :base(SymbolType.path, -1)
        {
            this.steps = steps;
        }

        public bool Equals(PathRuntimeNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            PathRuntimeNode otherPath = (PathRuntimeNode)other;
            return this.keepSingletonArray == otherPath.keepSingletonArray
                && Enumerable.SequenceEqual(this.steps, otherPath.steps);
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            if (this.keepSingletonArray)
            {
                sb.Append(", keepSingletonArray");
            }
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("steps[").Append(this.steps.Count).Append("]:\n");
            foreach (Node arg in this.steps)
            {
                arg.PrintAstInternal(sb, indent + 1);
            }
        }
    }

    public sealed class OrderbyConstructionNode: Node, IEquatable<OrderbyConstructionNode>
    {
        // LHS is the array to be ordered
        // RHS defines the terms
        public readonly Node lhs;
        public readonly List<SortTermNode> rhsTerms;

        public OrderbyConstructionNode(Node lhs, List<SortTermNode> rhsTerms, int position = -1)
            :base(SymbolType._binary_orderby, position)
        {
            this.rhsTerms = rhsTerms;
            this.lhs = lhs;
        }

        public bool Equals(OrderbyConstructionNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            OrderbyConstructionNode orderbyNode = (OrderbyConstructionNode)other;
            return this.lhs.Equals(orderbyNode.lhs)
                && Enumerable.SequenceEqual(this.rhsTerms, orderbyNode.rhsTerms);
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("lhs:\n");
            this.lhs.PrintAstInternal(sb, indent + 1);
            this.PrintIndent(sb, indent).Append("rhsTerms[").Append(this.rhsTerms.Count).Append("]:\n");
            foreach (Node arg in this.rhsTerms)
            {
                arg.PrintAstInternal(sb, indent + 1);
            }
        }
    }

    public sealed class TransformNode: Node, IEquatable<TransformNode>
    {
        public readonly Node pattern;
        public readonly Node update;
        public readonly Node? delete;

        public TransformNode(Node pattern, Node update, Node? delete, int position = -1)
            :base(SymbolType.transform, position)
        {
            this.pattern = pattern;
            this.update = update;
            this.delete = delete;
        }

        public bool Equals(TransformNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            TransformNode otherTransform = (TransformNode)other;
            if (!this.pattern.Equals(otherTransform.pattern))
            {
                return false;
            }
            if (!this.update.Equals(otherTransform.update))
            {
                return false;
            }
            if (!IsNullMatches(this.delete, otherTransform.delete))
            {
                return false;
            }
            if (this.delete != null && otherTransform.delete != null
                && !this.delete.Equals(otherTransform.delete))
            {
                return false;
            }
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("pattern:\n");
            this.pattern.PrintAstInternal(sb, indent + 1);
            this.PrintIndent(sb, indent).Append("update:\n");
            this.update.PrintAstInternal(sb, indent + 1);
            if (this.delete != null)
            {
                this.PrintIndent(sb, indent).Append("delete:\n");
                this.delete.PrintAstInternal(sb, indent + 1);
            }
        }
    }

    public sealed class ConditionNode: Node, IEquatable<ConditionNode>
    {
        // Ternary operator:
        public readonly Node condition;
        public readonly Node then;
        public readonly Node? @else;

        public ConditionNode(Node condition, Node then, Node? @else, int position = -1)
            :base(SymbolType.condition, position: position)
        {
            this.condition = condition;
            this.then = then;
            this.@else = @else;
        }

        public bool Equals(ConditionNode? other)
        {
            return this.EqualsImpl(other);
        }

        protected override bool EqualsSpecific(Node other)
        {
            ConditionNode otherCondition = (ConditionNode)other;
            if (!this.condition.Equals(otherCondition.condition))
            {
                return false;
            }
            if (!this.then.Equals(otherCondition.then))
            {
                return false;
            }
            if (!IsNullMatches(this.@else, otherCondition.@else))
            {
                return false;
            }
            if (this.@else != null && otherCondition.@else != null
                && !this.@else.Equals(otherCondition.@else))
            {
                return false;
            }
            return true;
        }

        protected override void PrintAstSpecific(StringBuilder sb)
        {
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("condition:\n");
            this.condition.PrintAstInternal(sb, indent + 1);
            this.PrintIndent(sb, indent).Append("then:\n");
            this.then.PrintAstInternal(sb, indent + 1);
            if (this.@else != null)
            {
                this.PrintIndent(sb, indent).Append("else:\n");
                this.@else.PrintAstInternal(sb, indent + 1);
            }
        }
    }
}

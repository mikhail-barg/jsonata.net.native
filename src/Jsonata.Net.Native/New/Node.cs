using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Jsonata.Net.Native.New
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
        partial,    // "?" - partial function arg. TODO: why at all it's an operator?? let's make it specific type!
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
        public List<StageNode>? stages { get; internal set; }
        public List<FilterNode>? predicate { get; internal set; }

        // Ancestor attributes
        internal SlotNode? ancestor;
        internal List<SlotNode>? seekingParent;
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

        public bool Equals(Node? other)
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

            return this.EqualsSpecific(other);
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
                foreach (StageNode stage in this.stages)
                {
                    stage.PrintAstInternal(sb, indent + 2);
                }
            }
            if (this.predicate != null)
            {
                this.PrintIndent(sb, indent + 1).Append("predicate[").Append(this.predicate.Count).Append("]\n");
                foreach (FilterNode stage in this.predicate)
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

    public sealed class EndNode : Node
    {
        public EndNode(int position)
            : base(SymbolType._end, position)
        {
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

    public sealed class BinaryFilterNode : Node
    {
        public readonly Node lhs;
        public readonly Node rhs;

        public BinaryFilterNode(int position, Node lhs, Node rhs)
            : base(SymbolType._binary_filter_node, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        protected override bool EqualsSpecific(Node other)
        {
            BinaryFilterNode otherBinary = (BinaryFilterNode)other;
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

    public sealed class BinaryPathNode : Node
    {
        public readonly Node lhs;
        public readonly Node rhs;

        public BinaryPathNode(int position, Node lhs, Node rhs)
            : base(SymbolType._binary_path_node, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        protected override bool EqualsSpecific(Node other)
        {
            BinaryPathNode otherBinary = (BinaryPathNode)other;
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

    public sealed class DescendantNode : Node
    {
        public DescendantNode(int position)
            : base(SymbolType.descendant, position)
        {
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

    public sealed class ParentNode : Node
    {
        public ParentNode(int position)
            : base(SymbolType._parent, position)
        {
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

    public sealed class ParentWithSlotNode : Node
    {
        public readonly SlotNode slot;
        public ParentWithSlotNode(int position, SlotNode slot)
            : base(SymbolType.parent, position)
        {
            this.slot = slot;
        }

        protected override bool EqualsSpecific(Node other)
        {
            ParentWithSlotNode otherParent = (ParentWithSlotNode)other;
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

    public sealed class WildcardNode: Node
    {
        public WildcardNode(int position)
            : base(SymbolType.wildcard, position)
        {
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

    public sealed class RegexNode : Node
    {
        public readonly Regex regex;

        public RegexNode(int position, Regex regex)
            : base(SymbolType.regex, position)
        {
            this.regex = regex;
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

    public sealed class NameNode : NodeWithStrValue
    {
        public NameNode(int position, string value)
            : base(SymbolType.name, position, value)
        {
        }
    }

    public sealed class VariableNode : NodeWithStrValue
    {
        public VariableNode(int position, string value)
            : base(SymbolType.variable, position, value)
        {
        }
    }

    public sealed class BindAssignVarNode : Node
    {
        public readonly VariableNode lhs;
        public readonly Node rhs;

        public BindAssignVarNode(int position, VariableNode lhs, Node rhs)
            : base(SymbolType._binary_bind_assign, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        protected override bool EqualsSpecific(Node other)
        {
            BindAssignVarNode otherBind = (BindAssignVarNode)other;
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

    public sealed class BindPositionalVarNode : Node
    {
        public readonly Node lhs;
        public readonly VariableNode rhs;

        public BindPositionalVarNode(int position, Node lhs, VariableNode rhs)
            : base(SymbolType._binary_bind_positional, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        protected override bool EqualsSpecific(Node other)
        {
            BindPositionalVarNode otherBind = (BindPositionalVarNode)other;
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

    public sealed class BindContextVarNode : Node
    {
        public readonly Node lhs;
        public readonly VariableNode rhs;

        public BindContextVarNode(int position, Node lhs, VariableNode rhs)
            : base(SymbolType._binary_bind_context, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        protected override bool EqualsSpecific(Node other)
        {
            BindContextVarNode otherBind = (BindContextVarNode)other;
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

    public sealed class OperatorNode : Node
    {
        public readonly SpecialOperatorType value;

        public OperatorNode(int position, SpecialOperatorType value)
            : base(SymbolType.@operator, position)
        {
            this.value = value;
        }

        protected override bool EqualsSpecific(Node other)
        {
            OperatorNode otherOperator = (OperatorNode)other;
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

    public sealed class StringNode : Node
    {
        public readonly string value;

        public StringNode(int position, string value)
            : base(SymbolType.@string, position)
        {
            this.value = value;
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

    public sealed class ValueNullNode : Node
    {
        public ValueNullNode(int position)
            : base(SymbolType._value_null, position)
        {
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

    public sealed class ValueBoolNode : Node
    {
        public readonly bool value;

        public ValueBoolNode(int position, bool value)
            : base(SymbolType._value_bool, position)
        {
            this.value = value;
        }

        protected override bool EqualsSpecific(Node other)
        {
            ValueBoolNode otherBool = (ValueBoolNode)other;
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

    public sealed class NumberIntNode : Node
    {
        public readonly long value;

        public NumberIntNode(int position, long value)
            :base(SymbolType._number_int, position)
        {
            this.value = value;
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

    public sealed class NumberDoubleNode : Node
    {
        public readonly double value;

        public NumberDoubleNode(int position, double value)
            : base(SymbolType._number_double, position)
        {
            this.value = value;
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
    public sealed class LambdaNode : Node
    {
        public readonly List<VariableNode> arguments;
        public readonly Signature? signature;
        public readonly Node body;
        public readonly bool thunk;

        public LambdaNode(int position, List<VariableNode> arguments, Signature? signature, Node body, bool thunk)
            :base(SymbolType.lambda, position)
        {
            this.arguments = arguments;
            this.signature = signature;
            this.body = body;
            this.thunk = thunk;
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
            this.PrintIndent(sb, indent).Append("args[").Append(this.arguments.Count).Append("]:\n");
            foreach (VariableNode arg in this.arguments)
            {
                arg.PrintAstInternal(sb, indent + 1);
            }
            this.PrintIndent(sb, indent).Append("body:\n");
            this.body.PrintAstInternal(sb, indent + 1);
        }
    }

    public sealed class FunctionalNode: Node
    {
        public readonly Node procedure;
        public readonly List<Node> arguments;

        public FunctionalNode(SymbolType type, int position, Node procedure, List<Node> arguments)
            :base(type, position)
        {
            switch (type)
            {
            case SymbolType.function:
            case SymbolType.partial:
                break;
            default:
                throw new Exception($"Unexpected function type {type}");
            }
            this.procedure = procedure;
            this.arguments = arguments;
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
            //nothing to do
        }

        protected override void PrintAstChildren(StringBuilder sb, int indent)
        {
            this.PrintIndent(sb, indent).Append("args[").Append(this.arguments.Count).Append("]:\n");
            foreach (VariableNode arg in this.arguments)
            {
                arg.PrintAstInternal(sb, indent + 1);
            }
            this.PrintIndent(sb, indent).Append("procedure:\n");
            this.procedure.PrintAstInternal(sb, indent + 1);
        }

    }

    public sealed class SlotNode: Node
    {
        public string label;
        public int level;
        public readonly int ancestorIndex;

        public SlotNode(string label, int ancestorIndex, int level)
            : base(SymbolType._slot, -1)
        {
            this.label = label;
            this.ancestorIndex = ancestorIndex;
            this.level = level;
        }

        protected override bool EqualsSpecific(Node other)
        {
            SlotNode otherSlot = (SlotNode)other;
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

    public sealed class UnaryMinusNode: Node
    {
        public readonly Node expression;

        public UnaryMinusNode(int position, Node expression)
            : base(SymbolType._unary_minus, position)
        {
            this.expression = expression;
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

    public sealed class SortTermNode: Node
    {
        public readonly Node expression;
        public readonly bool descending;

        public SortTermNode(int position, Node expression, bool descending)
            :base(SymbolType._sort_term, position)
        {
            this.expression = expression;
            this.descending = descending;
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

    public sealed class SortNode: Node
    {
        public readonly List<SortTermNode> terms;

        public SortNode(int position, List<SortTermNode> terms)
            :base(SymbolType.sort, position) 
        { 
            this.terms = terms;
        }

        protected override bool EqualsSpecific(Node other)
        {
            SortNode otherSort = (SortNode)other;
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

    public abstract class StageNode : Node
    {
        internal StageNode(SymbolType type, int position) 
            : base(type, position)
        {
        }
    }

    public sealed class FilterNode: StageNode
    {
        public readonly Node expr;

        public FilterNode(int position, Node expr)
            :base(SymbolType.filter, position)
        {
            this.expr = expr;
        }

        protected override bool EqualsSpecific(Node other)
        {
            FilterNode otherFilt = (FilterNode)other;
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

    public sealed class IndexNode : StageNode
    {
        public readonly string indexValue;

        public IndexNode(int position, string indexValue)
            : base(SymbolType.index, position)
        {
            this.indexValue = indexValue;
        }

        protected override bool EqualsSpecific(Node other)
        {
            IndexNode otherIndex = (IndexNode)other;
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

    public sealed class BlockNode: Node
    {
        public readonly List<Node> expressions;

        public BlockNode(int position, List<Node> expressions)
            : base(SymbolType.block, position)
        {
            this.expressions = expressions;
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

    public sealed class ArrayNode : Node
    {
        public readonly List<Node> expressions;

        public ArrayNode(int position, List<Node> expressions)
            : base(SymbolType._unary_array, position)
        {
            this.expressions = expressions;
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

    public sealed class ApplyNode : Node
    {
        public readonly Node lhs;
        public readonly Node rhs;

        public ApplyNode(int position, Node lhs, Node rhs)
            : base(SymbolType.apply, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
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

    public sealed class BindNode : Node
    {
        public readonly VariableNode lhs;
        public readonly Node rhs;

        public BindNode(int position, VariableNode lhs, Node rhs)
            : base(SymbolType.bind, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        protected override bool EqualsSpecific(Node other)
        {
            BindNode otherBind = (BindNode)other;
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

    public sealed class GroupNode : Node
    {
        public readonly List<Tuple<Node, Node>> lhsObject;

        public GroupNode(int position, List<Tuple<Node, Node>> lhsObject)
            : base(SymbolType._unary_group, position)
        {
            this.lhsObject = lhsObject;
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

    public sealed class GroupByNode: Node
    {
        public readonly Node lhs;
        public readonly List<Tuple<Node, Node>> rhsObject;

        public GroupByNode(int position, Node lhs, List<Tuple<Node, Node>> rhsObject)
            : base(SymbolType._binary_groupby, position)
        {
            this.lhs = lhs;
            this.rhsObject = rhsObject;
        }

        protected override bool EqualsSpecific(Node other)
        {
            GroupByNode otherGroup = (GroupByNode)other;
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

    public sealed class BinaryNode: Node
    {
        public readonly Node lhs;
        public readonly Node rhs;
        public readonly BinaryOperatorType value;

        public BinaryNode(int position, BinaryOperatorType value, Node lhs, Node rhs)
            :base(SymbolType.binary, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
            this.value = value;
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

    public abstract class NodeWithStrValue: Node
    {
        public readonly string value;

        public NodeWithStrValue(SymbolType type, int position, string value)
            :base(type, position)
        {
            this.value = value;
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

    public sealed class PathNode: Node
    {
        public readonly List<Node> steps;
        public bool keepSingletonArray { get; internal set; } = false;

        public PathNode(List<Node> steps)
            :base(SymbolType.path, -1)
        {
            this.steps = steps;
        }

        protected override bool EqualsSpecific(Node other)
        {
            PathNode otherPath = (PathNode)other;
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

    public sealed class OrderbyNode: Node
    {
        // LHS is the array to be ordered
        // RHS defines the terms
        public readonly Node lhs;
        public readonly List<SortTermNode> rhsTerms;

        public OrderbyNode(int position, Node lhs, List<SortTermNode> rhsTerms)
            :base(SymbolType._binary_orderby, position)
        {
            this.rhsTerms = rhsTerms;
            this.lhs = lhs;
        }

        protected override bool EqualsSpecific(Node other)
        {
            OrderbyNode orderbyNode = (OrderbyNode)other;
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

    public sealed class TransformNode: Node
    {
        public readonly Node pattern;
        public readonly Node update;
        public readonly Node? delete;

        public TransformNode(int position, Node pattern, Node update, Node? delete)
            :base(SymbolType.transform, position)
        {
            this.pattern = pattern;
            this.update = update;
            this.delete = delete;
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

    public sealed class ConditionNode: Node
    {
        // Ternary operator:
        public readonly Node condition;
        public readonly Node then;
        public readonly Node? @else;

        public ConditionNode(int position, Node condition, Node then, Node? @else)
            :base(SymbolType.condition, position: position)
        {
            this.condition = condition;
            this.then = then;
            this.@else = @else;
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

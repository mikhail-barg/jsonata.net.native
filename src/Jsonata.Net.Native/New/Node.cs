using System;
using System.Collections.Generic;
using System.IO;
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

    public class Node
    {
        public readonly SymbolType type;
        internal readonly int position;

        public bool tuple { get; internal set; } = false;
        public bool consarray { get; internal set; } = false;
        internal bool keepArray;
        public GroupNode? group { get; set; }

        // Ancestor attributes
        internal SlotNode? ancestor;
        internal List<SlotNode>? seekingParent;

        internal string? index_string;
        internal string? focus;

        internal List<StageNode>? stages;
        internal List<FilterNode>? predicate;

        internal Node(SymbolType type, int position)
        {
            this.type = type;
            this.position = position;
        }

        internal Node(Token token, SymbolType type)
            :this(type, token.position)
        {
        }

        public override string ToString()
        {
            return $"{this.GetType().Name} {this.type}";
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
    }

    public sealed class DescendantNode : Node
    {
        public DescendantNode(int position)
            : base(SymbolType.descendant, position)
        {
        }
    }

    public sealed class ParentNode : Node
    {
        public ParentNode(int position)
            : base(SymbolType._parent, position)
        {
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
    }

    public sealed class WildcardNode: Node
    {
        public WildcardNode(int position)
            : base(SymbolType.wildcard, position)
        {
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
    }

    public sealed class NameNode : NodeWithStrValue
    {
        public NameNode(int position, string value)
            : base(SymbolType.name, position, value)
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
    }

    public sealed class VariableNode : NodeWithStrValue
    {
        public VariableNode(int position, string value)
            : base(SymbolType.variable, position, value)
        {
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
    }

    public sealed class StringNode : Node
    {
        public readonly string value;

        public StringNode(int position, string value)
            : base(SymbolType.@string, position)
        {
            this.value = value;
        }
    }

    public sealed class ValueNullNode : Node
    {
        public ValueNullNode(int position)
            : base(SymbolType._value_null, position)
        {
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
    }

    public sealed class NumberIntNode : Node
    {
        public readonly long value;

        public NumberIntNode(int position, long value)
            :base(SymbolType._number_int, position)
        {
            this.value = value;
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
    }

    public sealed class SlotNode: Node
    {
        public string label;
        public int level;
        public readonly int index_int;


        public SlotNode(string label, int index_int, int level)
            : base(SymbolType._slot, -1)
        {
            this.label = label;
            this.index_int = index_int;
            this.level = level;
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
    }

    public sealed class SortNode: Node
    {
        public readonly List<SortTermNode> terms;

        public SortNode(int position, List<SortTermNode> terms)
            :base(SymbolType.sort, position) 
        { 
            this.terms = terms;
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
    }

    public sealed class IndexNode : StageNode
    {
        public readonly string indexValue;

        public IndexNode(int position, string indexValue)
            : base(SymbolType.index, position)
        {
            this.indexValue = indexValue;
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
    }

    public sealed class ArrayNode : Node
    {
        public readonly List<Node> expressions;

        public ArrayNode(int position, List<Node> expressions)
            : base(SymbolType._unary_array, position)
        {
            this.expressions = expressions;
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
    }

    public sealed class GroupNode : Node
    {
        public readonly List<Tuple<Node, Node>> lhsObject;

        public GroupNode(int position, List<Tuple<Node, Node>> lhsObject)
            : base(SymbolType._unary_group, position)
        {
            this.lhsObject = lhsObject;
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
    }

    public abstract class NodeWithStrValue: Node
    {
        public readonly string value;

        public NodeWithStrValue(SymbolType type, int position, string value)
            :base(type, position)
        {
            this.value = value;
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
    }
}

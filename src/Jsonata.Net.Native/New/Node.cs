using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        parent,
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
        _binary_orderby,    //was a part of 'binary', gets optimized away during processAST() phase
        _binary_groupby,    //was a part of 'binary', gets optimized away during processAST() phase
        _unary_group,       //was a part of 'unary'
        _unary_minus,       //was a part of 'unary'
        _unary_array,       //was a part of 'unary'
        _number_int,        //was a part of 'number'
        _number_double,     //was a part of 'number'
        _value_bool,        //was a part of 'value
        _value_null,        //was a part of 'value
    }

    public enum OperatorType
    {
        range,      // double-dot .. range operator
        assignment, // := assignment
        ne,         // !=
        ge,         // >=
        le,         // <=
        descendant, // **  descendant wildcard
        chain,      // ~>  chain function
        elvis,      // ?: default / elvis operator
        coalescing, // ?? coalescing operator
        dot,            // .
        bracket_square_open,    // [
        bracket_square_close,   // ]
        bracket_curly_open,     // {
        bracket_curly_close,    // }
        bracket_round_open,     // (
        bracket_round_close,    // )
        comma,                  // ,
        context_var_bind,       // @
        positional_var_bind,    // #
        semicolon,              // ;
        colon,                  // :
        question,               // ?
        plus,                   // +
        minus,                  // -
        mul,                    // *
        div,                    // /
        mod,                    // %
        transform,              // |
        eq,                     // =
        lt,                     // <
        gt,                     // >
        sort,                   // ^
        and,    // and
        or,     // or
        @in,    // in
        concat, // &
        excl,   // !    - not sure what it does
        tilda,  // ~    - not sure what it does
    }

    public class Node
    {
        public readonly SymbolType type;
        internal readonly object? value;
        internal readonly int position;

        public bool tuple { get; internal set; } = false;
        public bool consarray { get; internal set; } = false;
        internal bool keepArray;
        public GroupNode? group { get; set; }

        // Ancestor attributes
        internal SlotNode? ancestor;
        internal SlotNode? slot;
        internal List<SlotNode>? seekingParent;

        internal string? index_string;
        internal string? focus;

        internal List<StageNode>? stages;
        internal List<FilterNode>? predicate;

        internal Node(SymbolType type, object? value, int position)
        {
            this.type = type;
            this.value = value;
            this.position = position;
        }

        internal Node(Token token, SymbolType type)
            :this(type, token.value, token.position)
        {
        }

        public override string ToString()
        {
            return $"{this.GetType().Name} {this.type} value={this.value}";
        }
    }

    public sealed class StringNode : Node
    {
        public new readonly string value;

        public StringNode(int position, string value)
            : base(SymbolType.@string, null, position)
        {
            this.value = value;
        }
    }

    public sealed class ValueNullNode : Node
    {
        public ValueNullNode(int position)
            : base(SymbolType._value_null, null, position)
        {
        }
    }

    public sealed class ValueBoolNode : Node
    {
        public new readonly bool value;

        public ValueBoolNode(int position, bool value)
            : base(SymbolType._value_bool, null, position)
        {
            this.value = value;
        }
    }

    public sealed class NumberIntNode : Node
    {
        public new readonly long value;

        public NumberIntNode(int position, long value)
            :base(SymbolType._number_int, null, position)
        {
            this.value = value;
        }
    }

    public sealed class NumberDoubleNode : Node
    {
        public new readonly double value;

        public NumberDoubleNode(int position, double value)
            : base(SymbolType._number_double, null, position)
        {
            this.value = value;
        }
    }
    public sealed class LambdaNode : Node
    {
        public readonly List<Node> arguments;
        public readonly Signature? signature;
        public readonly Node body;
        public readonly bool thunk;

        public LambdaNode(int position, List<Node> arguments, Signature? signature, Node body, bool thunk)
            :base(SymbolType.lambda, null, position)
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
            :base(type, null, position)
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
            : base(SymbolType._slot, null, -1)
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
            : base(SymbolType._unary_minus, null, position)
        {
            this.expression = expression;
        }
    }

    public sealed class SortTermNode: Node
    {
        public readonly Node expression;
        public readonly bool descending;

        public SortTermNode(int position, Node expression, bool descending)
            :base(SymbolType._sort_term, null, position)
        {
            this.expression = expression;
            this.descending = descending;
        }
    }

    public sealed class SortNode: Node
    {
        public readonly List<SortTermNode> terms;

        public SortNode(int position, List<SortTermNode> terms)
            :base(SymbolType.sort, null, position) 
        { 
            this.terms = terms;
        }
    }

    public abstract class StageNode : Node
    {
        internal StageNode(SymbolType type, object? value, int position) 
            : base(type, value, position)
        {
        }
    }

    public sealed class FilterNode: StageNode
    {
        public readonly Node expr;

        public FilterNode(int position, Node expr)
            :base(SymbolType.filter, null, position)
        {
            this.expr = expr;
        }
    }

    public sealed class IndexNode : StageNode
    {
        public readonly string indexValue;

        public IndexNode(int position, string indexValue)
            : base(SymbolType.index, null, position)
        {
            this.indexValue = indexValue;
        }
    }

    public sealed class BlockNode: Node
    {
        public readonly List<Node> expressions;

        public BlockNode(int position, List<Node> expressions)
            : base(SymbolType.block, null, position)
        {
            this.expressions = expressions;
        }
    }

    public sealed class ArrayNode : Node
    {
        public readonly List<Node> expressions;

        public ArrayNode(int position, List<Node> expressions)
            : base(SymbolType._unary_array, null, position)
        {
            this.expressions = expressions;
        }
    }

    public sealed class ApplyNode : Node
    {
        public readonly Node lhs;
        public readonly Node rhs;

        public ApplyNode(int position, Node lhs, Node rhs)
            : base(SymbolType.apply, null, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }
    }

    public sealed class BindNode : Node
    {
        public readonly Node lhs;
        public readonly Node rhs;

        public BindNode(int position, Node lhs, Node rhs)
            : base(SymbolType.bind, null, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }
    }

    public sealed class GroupNode : Node
    {
        public readonly List<Tuple<Node, Node>> lhsObject;

        public GroupNode(int position, List<Tuple<Node, Node>> lhsObject)
            : base(SymbolType._unary_group, null, position)
        {
            this.lhsObject = lhsObject;
        }
    }

    public sealed class GroupByNode: Node
    {
        public readonly Node lhs;
        public readonly List<Tuple<Node, Node>> rhsObject;

        public GroupByNode(int position, Node lhs, List<Tuple<Node, Node>> rhsObject)
            : base(SymbolType._binary_groupby, null, position)
        {
            this.lhs = lhs;
            this.rhsObject = rhsObject;
        }
    }

    public sealed class BinaryNode: Node
    {
        public readonly Node lhs;
        public readonly Node rhs;

        public BinaryNode(string value, int position, Node lhs, Node rhs)
            :base(SymbolType.binary, value, position)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }
    }

    public sealed class PathNode: Node
    {
        public readonly List<Node> steps;
        public bool keepSingletonArray { get; internal set; } = false;

        public PathNode(List<Node> steps)
            :base(SymbolType.path, null, -1)
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
            :base(SymbolType._binary_orderby, null, position)
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
            :base(SymbolType.transform, null, position)
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
            :base(SymbolType.condition, value: null, position: position)
        {
            this.condition = condition;
            this.then = then;
            this.@else = @else;
        }
    }
}

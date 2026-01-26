using System;
using System.Collections.Generic;
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
        number,
        name,
        value,
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
        @partial,
        transform,
        descendant,
        //error,
        index,

        //added in dotnet to make proper handling
        _end, 
        _term,
        _sort_term,
        _slot,
        _binary_orderby,    //was a part of 'binary', gets optimized away during processAST() phase
        _binary_groupby,    //was a part of 'binary', gets optimized away during processAST() phase
        _unary_group,       //was a part of 'unary'
        _unary_minus,       //was a part of 'unary'
        _unary_array,       //was a part of 'unary'
    }

    public class Node
    {
        public readonly SymbolType type;
        internal readonly object? value;
        internal readonly int position;

        public bool tuple { get; internal set; } = false;
        public bool consarray { get; internal set; } = false;
        public GroupNode? group { get; set; }


        // Ancestor attributes
        internal string? label;
        internal int? index_int;
        internal string? index_string;
        internal Node? ancestor;
        internal Node? slot;
        public List<Node>? seekingParent;


        internal List<Node>? stages;
        internal string? focus;
        internal Node? expr;
        internal Node? nextFunction;

        internal List<Node>? predicate;
        internal List<Node>? arguments;
        internal Node? body;

        // Procedure:
        internal Node? procedure;

        internal List<Node>? terms;


        internal Node? expression; // ^
        internal bool descending; // ^

        internal bool keepArray; // [
        internal int level;

        internal bool thunk;

        // Procedure:
        internal string? name;
        internal Signature? signature;

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

        internal void Format(string? prefix, StringBuilder builder, int indent)
        {
            static void FormatListIfExists(List<Node>? list, string name, StringBuilder builder, int indent)
            {
                if (list == null)
                {
                    return;
                }

                for (int i = 0; i < list.Count; ++i)
                {
                    list[i].Format($"{name}[{i}]: ", builder, indent);
                }
            }

            if (prefix != null)
            {
                builder.Append('\n');
            }

            for (int i = 0; i < indent; ++i)
            {
                builder.Append('\t');
            }

            if (prefix != null)
            {
                builder.Append(prefix);
            }
            builder.Append(this.GetType().Name).Append(' ')
                .Append(this.type.ToString()).Append(' ')
                .Append("pos=").Append(this.position).Append(' ')
                ;
            if (this.value != null)
            {
                builder.Append("value=").Append(this.value).Append(' ');
            }
            if (this.tuple)
            {
                builder.Append("tuple ");
            }
            if (this.consarray)
            {
                builder.Append("consarray ");
            }
            if (this.focus != null)
            {
                builder.Append("focus=").Append(this.focus).Append(' ');
            }
            if (this.label != null)
            {
                builder.Append("label=").Append(this.label).Append(' ');
            }
            if (this.index_int != null)
            {
                builder.Append("index(int)=").Append(this.index_int).Append(' ');
            }
            if (this.index_string != null)
            {
                builder.Append("index(str)=").Append(this.index_string).Append(' ');
            }
            if (this.keepArray)
            {
                builder.Append("keepArray=").Append(this.keepArray).Append(' ');
            }

            if (this.ancestor != null)
            {
                this.ancestor.Format("ancestor: ", builder, indent + 1);
            }

            if (this.slot != null)
            {
                this.slot.Format("slot: ", builder, indent + 1);
            }
            if (this.group != null)
            {
                this.group.Format("group: ", builder, indent + 1);
            }
            if (this.expr != null)
            {
                this.expr.Format("expr: ", builder, indent + 1);
            }
            if (this.nextFunction != null)
            {
                this.nextFunction.Format("nextFunction: ", builder, indent + 1);
            }
            if (this.body != null)
            {
                this.body.Format("body: ", builder, indent + 1);
            }
            FormatListIfExists(this.stages, "stages", builder, indent + 1);
            FormatListIfExists(this.predicate, "predicate", builder, indent + 1);
            FormatListIfExists(this.arguments, "arguments", builder, indent + 1);
            FormatListIfExists(this.seekingParent, "seekingParent", builder, indent + 1);
            FormatListIfExists(this.terms, "terms", builder, indent + 1);
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
        public readonly List<Node> rhsTerms;

        public OrderbyNode(int position, Node lhs, List<Node> rhsTerms)
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

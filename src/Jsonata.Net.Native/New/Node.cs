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
        unary,
        function,
        lambda,
        condition,
        block,
        wildcard,
        parent,
        predicate,
        stages,
        filter,
        bind,
        apply,
        @partial,
        transform,
        descendant,
        error,
        index,

        //added in dotnet to make proper handling
        _end, 
        _term,
        _group,
        _sort_term,
        _slot
    }

    public class Node
    {
        public SymbolType type;
        internal List<Node>? steps;
        internal List<Node>? stages;
        internal bool tuple = false;
        internal bool consarray = false;
        internal string? focus;
        internal bool keepSingletonArray = false;
        internal Node? group;
        internal Node? expr;
        internal object? value;
        internal int position;
        internal Node? nextFunction;

        // Infix attributes
        public Node? lhs, rhs;

        internal List<Node>? predicate;
        internal List<Node>? arguments;
        internal Node? body;

        // Block
        internal List<Node>? expressions;

        // Ancestor attributes
        internal string? label;
        internal int? index_int;
        internal string? index_string;
        internal bool? _jsonata_lambda;
        internal Node? ancestor;

        internal Node? slot;

        public List<Node>? seekingParent;

        // Procedure:
        internal Node? procedure;

        internal List<Node>? terms;
        // where rhs = list of Symbols
        internal List<Node>? rhsTerms;

        internal Node? expression; // ^
        internal bool descending; // ^

        // where rhs = list of Symbol pairs
        // TODO: convert to Tuple
        internal List<Node[]>? lhsObject, rhsObject;

        // Prefix attributes
        internal Node? pattern, update, delete;

        internal bool keepArray; // [


        internal int level;
        internal bool thunk;

        // Procedure:
        internal string? name;


        internal Signature? signature;

        internal Node(SymbolType type)
        {
            this.type = type;
        }

        internal Node(Token token)
        {
            this.type = token.type;
            this.value = token.value;
            this.position = token.position;
        }

        internal Node(Token token, SymbolType type)
        {
            this.type = type;
            this.value = token.value;
            this.position = token.position;
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
            static void FormatList2IfExists(List<Node[]>? list, string name, StringBuilder builder, int indent)
            {
                if (list == null)
                {
                    return;
                }

                for (int i = 0; i < list.Count; ++i)
                {
                    for (int j = 0; j < list[i].Length; ++j)
                    {
                        list[i][j].Format($"{name}[{i}][{j}]: ", builder, indent);
                    }
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
            if (this.keepSingletonArray)
            {
                builder.Append("keepSingletonArray ");
            }
            if (this._jsonata_lambda != null)
            {
                builder.Append("_jsonata_lambda=").Append(this._jsonata_lambda).Append(' ');
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

            if (this.lhs != null)
            {
                this.lhs.Format("lhs: ", builder, indent + 1);
            }
            if (this.rhs != null)
            {
                this.rhs.Format("rhs: ", builder, indent + 1);
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
            FormatListIfExists(this.steps, "steps", builder, indent + 1);
            FormatListIfExists(this.stages, "stages", builder, indent + 1);
            FormatListIfExists(this.predicate, "predicate", builder, indent + 1);
            FormatListIfExists(this.arguments, "arguments", builder, indent + 1);
            FormatListIfExists(this.expressions, "expressions", builder, indent + 1);
            FormatListIfExists(this.seekingParent, "seekingParent", builder, indent + 1);
            FormatListIfExists(this.terms, "terms", builder, indent + 1);
            FormatListIfExists(this.rhsTerms, "rhsTerms", builder, indent + 1);
            FormatList2IfExists(this.lhsObject, "lhsObject", builder, indent + 1);
            FormatList2IfExists(this.rhsObject, "rhsObject", builder, indent + 1);
        }
    }

    internal class ConditionNode : Node
    {
        // Ternary operator:
        internal Node? condition;
        internal Node? then;
        internal Node? @else;

        internal ConditionNode()
            :base(SymbolType.condition)
        {

        }

        internal ConditionNode(Token token)
            :base(token, SymbolType.condition)
        {
        }
    }
}

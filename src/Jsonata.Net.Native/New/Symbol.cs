using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;

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
        index
    }

    public partial class Symbol
    {
        internal string id;
        internal SymbolType type;
        internal List<Symbol>? steps;
        internal List<Symbol>? stages;
        internal bool tuple = false;
        internal bool consarray = false;
        internal string? focus;
        internal bool keepSingletonArray = false;
        internal Symbol? group;
        internal Symbol? expr;
        internal object? value;
        internal int bp;
        internal int lbp;
        internal int position;
        internal Symbol? nextFunction;


        // Infix attributes
        public Symbol? lhs, rhs;

        internal List<Symbol>? predicate;
        internal List<Symbol>? arguments;
        internal Symbol? body;

        // Ternary operator:
        internal Symbol? condition;
        internal Symbol? then;
        internal Symbol? @else;

        // Block
        internal List<Symbol>? expressions;

        // Ancestor attributes
        internal String? label;
        internal Object? index; //TODO: int
        internal bool? _jsonata_lambda;
        internal Symbol? ancestor;

        internal Symbol? slot;

        public List<Symbol>? seekingParent;

        // Procedure:
        internal Symbol? procedure;

        internal List<Symbol>? terms;
        // where rhs = list of Symbols
        internal List<Symbol>? rhsTerms;

        internal Symbol? expression; // ^
        internal bool descending; // ^

        // where rhs = list of Symbol pairs
        // TODO: convert to Tuple
        internal List<Symbol[]>? lhsObject, rhsObject;

        // Prefix attributes
        internal Symbol? pattern, update, delete;

        internal bool keepArray; // [


        internal int level;
        //public Object token;
        internal bool thunk;

        // Procedure:
        //public Object input;
        //public EvaluationEnvironment environment;
        internal string? name;


        internal Signature? signature;


        virtual internal Symbol nud(Parser parser)
        {
            // error - symbol has been invoked as a unary operator
            throw new JException("S0211", this.position, this.value);
        }

        virtual internal Symbol led(Symbol left, Parser parser)
        {
            throw new Exception("led not implemented");
        }

        //class Symbol {
        public Symbol() 
            :this("", 0)
        {

        }

        public Symbol(string id) 
            :this(id, 0) 
        { 
        }

        public Symbol(string id, int bp)
        {
            this.id = id; 
            this.value = id;
            this.bp = bp;
            /* use register(Symbol) ! Otherwise inheritance doesn't work
                        Symbol s = symbolTable.get(id);
                        //bp = bp != 0 ? bp : 0;
                        if (s != null) {
                            if (bp >= s.lbp) {
                                s.lbp = bp;
                            }
                        } else {
                            s = new Symbol();
                            s.value = s.id = id;
                            s.lbp = bp;
                            symbolTable.put(id, s);
                        }

            */
            //return s;
        }

        public override string ToString()
        {
            return $"{this.GetType().Name} {this.id} value={this.value}";
        }
    }

    // match infix operators
    // <expression> <operator> <expression>
    // left associative
    internal class Infix: Symbol
    {
        internal Infix(string id)
            : this(id, 0)
        {

        }

        internal Infix(string id, int bp)
            :base(id, bp != 0 ? bp : (id != "" ? Tokenizer.operators[id] : 0))
        {
            
        }

        internal override Symbol led(Symbol left, Parser parser) 
        {
            this.lhs = left;
            this.rhs = parser.expression(bp);
            this.type = SymbolType.binary;
            return this;
        }
    }

    internal class InfixCustom : Infix
    {
        internal InfixCustom()
            : base("", 0)
        {

        }
    }

    internal class InfixWithNud : Infix
    {
        internal InfixWithNud(string id)
            : base(id, 0)
        {
        }

        //internal InfixWithNud(string id, int bp)
        //    : base(id, bp)
        //{
        //
        //}

        internal override Symbol nud(Parser parser)
        {
            return this;
        }
    }

    internal class InfixWithTypedNud : Infix
    {
        private readonly SymbolType m_symbolType;
        internal InfixWithTypedNud(string id, SymbolType symbolType)
            : base(id, 0)
        {
            this.m_symbolType = symbolType;
        }

        //internal InfixTyped(string id, int bp)
        //    : base(id, bp)
        //{
        //
        //}

        internal override Symbol nud(Parser parser)
        {
            this.type = this.m_symbolType;
            return this;
        }
    }

    internal class InfixCoalescing : Infix
    {
        internal InfixCoalescing(string id, int bp)
            : base(id, bp)
        {
        }

        internal override Symbol led(Symbol left, Parser parser)
        {
            this.type = SymbolType.condition;
            Symbol c = new Symbol();
            this.condition = c;
            {
                c.type = SymbolType.function;
                c.value = "(";
                Symbol p = new Symbol();
                c.procedure = p; 
                p.type = SymbolType.variable; 
                p.value = "exists";
                c.arguments = new List<Symbol>() { left };
            }
            this.then = left;
            this.@else = parser.expression(0);
            return this;
        }
    }

    internal class InfixOrderBy : Infix
    {
        internal InfixOrderBy(string id, int bp)
            : base(id, bp)
        {
        }

        internal override Symbol led(Symbol left, Parser parser)
        {
            parser.advance("(");
            List<Symbol> terms = new ();
            while (true) 
            {
                Symbol term = new Symbol();
                term.descending = false;

                if (parser.node.id == "<")
                {
                    // ascending sort
                    parser.advance("<");
                }
                else if (parser.node.id == ">")
                {
                    // descending sort
                    term.descending = true;
                    parser.advance(">");
                }
                else
                {
                    //unspecified - default to ascending
                }
                term.expression = parser.expression(0);
                terms.Add(term);
                if (parser.node.id != ",")
                {
                    break;
                }
                parser.advance(",");
            }
            parser.advance(")");
            this.lhs = left;
            this.rhsTerms = terms;
            this.type = SymbolType.binary;
            return this;
        }
    }

    internal class InfixBlock: Infix
    {
        internal InfixBlock(string id, int bp)
            : base(id, bp)
        {
        }

        // merged register(new Prefix("{") {
        internal override Symbol nud(Parser parser)
        {
            return parser.objectParser(null);
        }

        // register(new Infix("{", Tokenizer.operators.get("{")) {
        internal override Symbol led(Symbol left, Parser parser)
        {
            return parser.objectParser(left);
        }
    }

    internal class InfixFocus : Infix
    {
        internal InfixFocus(string id, int bp)
            : base(id, bp)
        {
        }

        internal override Symbol led(Symbol left, Parser parser)
        {
            this.lhs = left;
            this.rhs = parser.expression(Tokenizer.operators["@"]);
            if (this.rhs.type != SymbolType.variable)
            {
                throw new JException("S0214", this.rhs.position, "@");
            }
            this.type = SymbolType.binary;
            return this;
        }
    }

    internal class InfixIndex : Infix
    {
        internal InfixIndex(string id, int bp)
            : base(id, bp)
        {
        }

        internal override Symbol led(Symbol left, Parser parser)
        {
            this.lhs = left;
            this.rhs = parser.expression(Tokenizer.operators["#"]);
            if (this.rhs.type != SymbolType.variable)
            {
                throw new JException("S0214", this.rhs.position, "#");
            }
            this.type = SymbolType.binary;
            return this;
        }
    }

    internal class InfixTernary : Infix
    {
        internal InfixTernary(string id, int bp)
            : base(id, bp)
        {
        }

        internal override Symbol led(Symbol left, Parser parser)
        {
            this.type = SymbolType.condition;
            this.condition = left;
            this.then = parser.expression(0);
            if (parser.node.id == ":")
            {
                // else condition
                parser.advance(":");
                this.@else = parser.expression(0);
            }
            return this;
        }
    }

    internal class InfixElvis : Infix
    {
        internal InfixElvis(string id, int bp)
            : base(id, bp)
        {
        }

        internal override Symbol led(Symbol left, Parser parser)
        {
            this.type = SymbolType.condition;
            this.condition = left;
            this.then = left;
            this.@else = parser.expression(0);
            return this;
        }
    }

    internal sealed class Terminal: Symbol
    {
        internal Terminal(string id)
            :base(id, 0)
        {
            
        }

        internal override Symbol nud(Parser parser) 
        {
            return this;
        }
    }

    // match prefix operators
    // <operator> <expression>
    internal class Prefix: Symbol
    {
        //public List<Symbol[]> lhs;

        internal Prefix(string id) 
            :base(id)
        {
            //type = "unary";
        }

        //Symbol _expression;

        internal override Symbol nud(Parser parser) 
        {
            this.expression = parser.expression(70);
            this.type = SymbolType.unary;
            return this;
        }
    }

    internal class PrefixTransformer : Prefix
    {
        internal PrefixTransformer(string id)
            : base(id)
        {
        }

        internal override Symbol nud(Parser parser)
        {
            this.type = SymbolType.transform;
            this.pattern = parser.expression(0);
            parser.advance("|");
            this.update = parser.expression(0);
            if (parser.node.id == ",")
            {
                parser.advance(",");
                this.delete = parser.expression(0);
            }
            parser.advance("|");
            return this;
        }
    }

    // match infix operators
    // <expression> <operator> <expression>
    // right associative
    internal class InfixR: Symbol
    {
        internal InfixR(string id, int bp) 
            : base(id, bp)
        {
        }

        //abstract Object led();
    }

    internal class InfixRError : InfixR
    {
        internal InfixRError(string id, int bp) : base(id, bp)
        {
        }

        internal override Symbol led(Symbol left, Parser parser)
        {
            throw new NotSupportedException("TODO", null);
        }
    }

    internal class InfixRVariableBind : InfixR
    {
        internal InfixRVariableBind(string id, int bp) : base(id, bp)
        {
        }

        internal override Symbol led(Symbol left, Parser parser)
        {
            if (left.type != SymbolType.variable)
            {
                throw new JException("S0212", left.position, left.value);
            }
            this.lhs = left;
            this.rhs = parser.expression(Tokenizer.operators[":="] - 1); // subtract 1 from bindingPower for right associative operators
            this.type = SymbolType.binary;
            return this;
        }
    }

    internal class InfixInvocation : Infix
    {
        internal InfixInvocation(string id, int bp) : base(id, bp)
        {
        }

        internal override Symbol led(Symbol left, Parser parser)
        {
            // left is is what we are trying to invoke
            this.procedure = left;
            this.type = SymbolType.function;
            this.arguments = new ();
            if (parser.node.id != ")")
            {
                while (true)
                {
                    if (parser.node.type == SymbolType.@operator && parser.node.id == "?")
                    {
                        // partial function application
                        this.type = SymbolType.partial;
                        this.arguments.Add(parser.node);
                        parser.advance("?");
                    }
                    else
                    {
                        this.arguments.Add(parser.expression(0));
                    }
                    if (parser.node.id != ",")
                    {
                        break;
                    }
                    parser.advance(",");
                }
            }
            parser.advance(")", true);
            // if the name of the function is 'function' or λ, then this is function definition (lambda function)
            if (left.type == SymbolType.name && (left.value!.Equals("function") || left.value!.Equals("\u03BB")))
            {
                // all of the args must be VARIABLE tokens
                //int index = 0;
                foreach (Symbol arg in this.arguments)
                {
                    //this.arguments.forEach(function (arg, index) {
                    if (arg.type != SymbolType.variable)
                    {
                        throw new JException("S0208", arg.position, arg.value);
                    }
                    //index++;
                }
                this.type = SymbolType.lambda;
                // is the next token a '<' - if so, parse the function signature
                if (parser.node.id == "<")
                {
                    int depth = 1;
                    String sig = "<";
                    while (depth > 0 && parser.node.id != "{" && parser.node.id != "(end)")
                    {
                        Symbol tok = parser.advance();
                        if (tok.id == ">")
                        {
                            --depth;
                        }
                        else if (tok.id == "<")
                        {
                            ++depth;
                        }
                        sig += tok.value;
                    }
                    parser.advance(">");
                    this.signature = new Signature(sig, "lambda");
                }
                // parse the function body
                parser.advance("{");
                this.body = parser.expression(0);
                parser.advance("}");
            }
            return this;
        }

        // parenthesis - block expression
        // Note: in Java both nud and led are in same class!
        //register(new Prefix("(") {

        internal override Symbol nud(Parser parser)
        {
            List<Symbol> expressions = new ();
            while (parser.node.id != ")")
            {
                expressions.Add(parser.expression(0));
                if (parser.node.id != ";")
                {
                    break;
                }
                parser.advance(";");
            }
            parser.advance(")", true);
            this.type = SymbolType.block;
            this.expressions = expressions;
            return this;
        }
    }

    internal class InfixArray : Infix
    {
        internal InfixArray(string id, int bp) : base(id, bp)
        {
        }

        internal override Symbol nud(Parser parser)
        {
            List<Symbol> a = new ();
            if (parser.node.id != "]")
            {
                while (true)
                {
                    Symbol item = parser.expression(0);
                    if (parser.node.id == "..")
                    {
                        // range operator
                        Symbol range = new Symbol();
                        range.type = SymbolType.binary;
                        range.value = ".."; 
                        range.position = parser.node.position; 
                        range.lhs = item;
                        parser.advance("..");
                        range.rhs = parser.expression(0);
                        item = range;
                    }
                    a.Add(item);
                    if (parser.node.id != ",")
                    {
                        break;
                    }
                    parser.advance(",");
                }
            }
            parser.advance("]", true);
            this.expressions = a;
            this.type = SymbolType.unary;
            return this;
        }

        // filter - predicate or array index
        //register(new Infix("[", Tokenizer.operators.get("[")) {

        internal override Symbol led(Symbol left, Parser parser)
        {
            if (parser.node.id == "]")
            {
                // empty predicate means maintain singleton arrays in the output
                Symbol? step = left;
                while (step != null && step.type == SymbolType.binary && step.value!.Equals("["))
                {
                    step = ((Infix)step).lhs;
                }
                if (step == null)
                {
                    throw new Exception("TODO??");
                }
                step.keepArray = true;
                parser.advance("]");
                return left;
            }
            else
            {
                this.lhs = left;
                this.rhs = parser.expression(Tokenizer.operators["]"]);
                this.type = SymbolType.binary;
                parser.advance("]", true);
                return this;
            }
        }
    }

    internal class PrefixDescendantWindcard : Prefix
    {
        internal PrefixDescendantWindcard(string id) : base(id)
        {
        }

        internal override Symbol nud(Parser parser)
        {
            this.type = SymbolType.descendant;
            return this;
        }
    }


    internal class InfixAndPrefix : Infix
    {
        internal Prefix prefix;

        internal InfixAndPrefix(string id)
            : this(id, 0)
        {

        }

        internal InfixAndPrefix(string id, int bp)
            : base(id, bp)
        {
            this.prefix = new Prefix(id);
        }

        internal override Symbol nud(Parser parser)
        {
            return this.prefix.nud(parser);
            // expression(70);
            // type="unary";
            // return this;
        }
    }
}

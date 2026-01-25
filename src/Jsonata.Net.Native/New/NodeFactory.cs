using System;
using System.Collections.Generic;
using System.Text;

namespace Jsonata.Net.Native.New
{
    internal abstract class NodeFactoryBase
    {
        internal string id;
        internal int bp;

        public NodeFactoryBase(string id)
            : this(id, 0)
        {
        }

        public NodeFactoryBase(string id, int bp)
        {
            this.id = id;
            this.bp = bp;
        }

        virtual internal Node nud(Parser parser, Token token)
        {
            throw new JException("S0211", token.position, token.value);
        }

        virtual internal Node led(Node left, Parser parser, Token token)
        {
            throw new Exception("led not implemented");
        }
    }

    internal sealed class DummyNodeFactory : NodeFactoryBase
    {
        public DummyNodeFactory(string id) : base(id)
        {
        }
    }

    internal sealed class TerminalFactory : NodeFactoryBase
    {
        public TerminalFactory(string id) : base(id, 0)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            switch (this.id)
            {
            case "(name)":
                switch (token.type)
                {
                case SymbolType.name:
                    return new Node(token, SymbolType.name);
                case SymbolType.variable:
                    return new Node(token, SymbolType.variable);
                default:
                    throw new Exception($"{this.id} -> {token.type.ToString()}");
                }
            case "(literal)":
                switch (token.type)
                {
                case SymbolType.number:
                    return new Node(token, SymbolType.number);
                case SymbolType.@string:
                    return new Node(token, SymbolType.@string);
                case SymbolType.value:
                    return new Node(token, SymbolType.value);
                default:
                    throw new Exception($"{this.id} -> {token.type.ToString()}");
                }
            case "(end)":
                switch (token.type)
                {
                case SymbolType._end:
                    return new Node(token, SymbolType._end);
                default:
                    throw new Exception($"{this.id} -> {token.type.ToString()}");
                }
            case "(regex)":
                switch (token.type)
                {
                case SymbolType.regex:
                    return new Node(token, SymbolType.regex);
                default:
                    throw new Exception($"{this.id} -> {token.type.ToString()}");
                }
            default:
                throw new Exception($"{this.id} -> {token.type.ToString()}");
            }
        }
    }

    // match infix operators
    // <expression> <operator> <expression>
    // left associative
    internal class InfixFactory : NodeFactoryBase
    {
        internal InfixFactory(string id)
                    : this(id, 0)
        {

        }

        internal InfixFactory(string id, int bp)
            : base(id, bp != 0 ? bp : (id != "" ? Tokenizer.OPERATORS[id] : 0))
        {

        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            Node symbol = new Node(token, SymbolType.binary);
            symbol.lhs = left;
            symbol.rhs = parser.expression(bp);
            return symbol;
        }
    }

    internal sealed class InfixWithTypedNudFactory : InfixFactory
    {
        private readonly SymbolType m_symbolType;

        public InfixWithTypedNudFactory(string id, SymbolType symbolType) : base(id, 0)
        {
            this.m_symbolType = symbolType;
        }

        internal override Node nud(Parser parser, Token token)
        {
            Node symbol = new Node(token, this.m_symbolType);
            return symbol;
        }
    }

    // match prefix operators
    // <operator> <expression>
    internal class PrefixFactory : NodeFactoryBase
    {
        internal PrefixFactory(string id)
            : base(id)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            Node symbol = new Node(token, SymbolType.unary);
            symbol.expression = parser.expression(70);
            return symbol;
        }
    }

    internal class PrefixTransformerFactory : PrefixFactory
    {
        internal PrefixTransformerFactory(string id)
            : base(id)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            Node symbol = new Node(token, SymbolType.transform);
            symbol.pattern = parser.expression(0);
            parser.advance("|");
            symbol.update = parser.expression(0);
            if (parser.currentNodeFactory.id == ",")
            {
                parser.advance(",");
                symbol.delete = parser.expression(0);
            }
            parser.advance("|");
            return symbol;
        }
    }

    internal class PrefixDescendantWindcardFactory : PrefixFactory
    {
        internal PrefixDescendantWindcardFactory(string id) : base(id)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            Node symbol = new Node(token, SymbolType.descendant);
            return symbol;
        }
    }

    internal sealed class InfixAndPrefixFactory : InfixFactory
    {
        internal PrefixFactory prefix;

        internal InfixAndPrefixFactory(string id)
            : this(id, 0)
        {

        }

        internal InfixAndPrefixFactory(string id, int bp)
            : base(id, bp)
        {
            this.prefix = new PrefixFactory(id);
        }

        internal override Node nud(Parser parser, Token token)
        {
            return this.prefix.nud(parser, token);
            // expression(70);
            // type="unary";
            // return this;
        }
    }

    internal class InfixOrderByFactory : InfixFactory
    {
        internal InfixOrderByFactory(string id, int bp)
            : base(id, bp)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            parser.advance("(");
            List<Node> terms = new();
            while (true)
            {
                Node term = new Node(SymbolType._term);
                term.descending = false;

                if (parser.currentNodeFactory.id == "<")
                {
                    // ascending sort
                    parser.advance("<");
                }
                else if (parser.currentNodeFactory.id == ">")
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
                if (parser.currentNodeFactory.id != ",")
                {
                    break;
                }
                parser.advance(",");
            }
            parser.advance(")");
            Node symbol = new Node(token, SymbolType.binary);
            symbol.lhs = left;
            symbol.rhsTerms = terms;
            return symbol;
        }
    }

    internal class InfixBlockFactory : InfixFactory
    {
        internal InfixBlockFactory(string id, int bp)
            : base(id, bp)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            return parser.objectParser(null);
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            return parser.objectParser(left);
        }
    }

    internal class InfixFocusFactory : InfixFactory
    {
        internal InfixFocusFactory(string id, int bp)
            : base(id, bp)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            Node symbol = new Node(token, SymbolType.binary);
            symbol.lhs = left;
            symbol.rhs = parser.expression(Tokenizer.OPERATORS["@"]);
            if (symbol.rhs.type != SymbolType.variable)
            {
                throw new JException("S0214", symbol.rhs.position, "@");
            }
            return symbol;
        }
    }

    internal class InfixIndexFactory : InfixFactory
    {
        internal InfixIndexFactory(string id, int bp)
            : base(id, bp)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            Node symbol = new Node(token, SymbolType.binary);
            symbol.lhs = left;
            symbol.rhs = parser.expression(Tokenizer.OPERATORS["#"]);
            if (symbol.rhs.type != SymbolType.variable)
            {
                throw new JException("S0214", symbol.rhs.position, "#");
            }
            return symbol;
        }
    }

    internal class InfixInvocationFactory : InfixFactory
    {
        internal InfixInvocationFactory(string id, int bp) : base(id, bp)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            SymbolType type = SymbolType.function;
            List<Node> arguments = new();
            Signature? signature = null;
            Node? body = null;
            if (parser.currentNodeFactory.id != ")")
            {
                while (true)
                {
                    if (parser.currentToken.type == SymbolType.@operator && parser.currentNodeFactory.id == "?")
                    {
                        // partial function application
                        type = SymbolType.partial;
                        //symbol.arguments.Add(parser.current_symbol); //TODO:convert to symbol!
                        arguments.Add(new Node(SymbolType.@operator) { value = "?" });
                        parser.advance("?");
                    }
                    else
                    {
                        arguments.Add(parser.expression(0));
                    }
                    if (parser.currentNodeFactory.id != ",")
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
                foreach (Node arg in arguments)
                {
                    //this.arguments.forEach(function (arg, index) {
                    if (arg.type != SymbolType.variable)
                    {
                        throw new JException("S0208", arg.position, arg.value);
                    }
                    //index++;
                }
                type = SymbolType.lambda;
                // is the next token a '<' - if so, parse the function signature
                if (parser.currentNodeFactory.id == "<")
                {
                    int depth = 1;
                    String sig = "<";
                    while (depth > 0 && parser.currentNodeFactory.id != "{" && parser.currentNodeFactory.id != "(end)")
                    {
                        parser.advance();
                        NodeFactoryBase tok = parser.currentNodeFactory;
                        if (tok.id == ">")
                        {
                            --depth;
                        }
                        else if (tok.id == "<")
                        {
                            ++depth;
                        }
                        sig += parser.currentToken.value;
                    }
                    parser.advance(">");
                    signature = new Signature(sig);
                }
                // parse the function body
                parser.advance("{");
                body = parser.expression(0);
                parser.advance("}");
            }
            Node symbol = new Node(token, type);
            // left is is what we are trying to invoke
            symbol.procedure = left;
            symbol.arguments = arguments;
            symbol.signature = signature;
            symbol.body = body;
            return symbol;
        }

        internal override Node nud(Parser parser, Token token)
        {
            List<Node> expressions = new();
            while (parser.currentNodeFactory.id != ")")
            {
                expressions.Add(parser.expression(0));
                if (parser.currentNodeFactory.id != ";")
                {
                    break;
                }
                parser.advance(";");
            }
            parser.advance(")", true);
            Node symbol = new Node(token, SymbolType.block);
            symbol.expressions = expressions;
            return symbol;
        }
    }

    internal class InfixArrayFactory : InfixFactory
    {
        internal InfixArrayFactory(string id, int bp) : base(id, bp)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            List<Node> a = new();
            if (parser.currentNodeFactory.id != "]")
            {
                while (true)
                {
                    Node item = parser.expression(0);
                    if (parser.currentNodeFactory.id == "..")
                    {
                        // range operator
                        Node range = new Node(SymbolType.binary);
                        range.value = "..";
                        //TODO: position?
                        //range.position = parser.current_symbol.position;
                        range.lhs = item;
                        parser.advance("..");
                        range.rhs = parser.expression(0);
                        item = range;
                    }
                    a.Add(item);
                    if (parser.currentNodeFactory.id != ",")
                    {
                        break;
                    }
                    parser.advance(",");
                }
            }
            parser.advance("]", true);
            Node symbol = new Node(token, SymbolType.unary);
            symbol.expressions = a;
            return symbol;
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            if (parser.currentNodeFactory.id == "]")
            {
                // empty predicate means maintain singleton arrays in the output
                Node? step = left;
                while (step != null && step.type == SymbolType.binary && step.value!.Equals("["))
                {
                    step = step.lhs;
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
                Node symbol = new Node(token, SymbolType.binary);
                symbol.lhs = left;
                symbol.rhs = parser.expression(Tokenizer.OPERATORS["]"]);
                parser.advance("]", true);
                return symbol;
            }
        }
    }

    internal abstract class InfixConditionFactory : InfixFactory
    {
        internal InfixConditionFactory(string id, int bp)
            : base(id, bp)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            throw new Exception("Should not happen");
        }
    }

    internal class InfixCoalescingFactory : InfixConditionFactory
    {
        internal InfixCoalescingFactory(string id, int bp)
            : base(id, bp)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            ConditionNode symbol = new ConditionNode(token);
            Node c = new Node(SymbolType.function);
            symbol.condition = c;
            c.value = "(";
            c.arguments = new List<Node>() { left };
            Node p = new Node(SymbolType.variable);
            c.procedure = p;
            p.value = "exists";
            symbol.then = left;
            symbol.@else = parser.expression(0);
            return symbol;
        }
    }

    internal class InfixTernaryFactory : InfixConditionFactory
    {
        internal InfixTernaryFactory(string id, int bp)
            : base(id, bp)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            ConditionNode symbol = new ConditionNode(token);
            symbol.condition = left;
            symbol.then = parser.expression(0);
            if (parser.currentNodeFactory.id == ":")
            {
                // else condition
                parser.advance(":");
                symbol.@else = parser.expression(0);
            }
            return symbol;
        }
    }

    internal class InfixElvisFactory : InfixConditionFactory
    {
        internal InfixElvisFactory(string id, int bp)
            : base(id, bp)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            ConditionNode symbol = new ConditionNode(token);
            symbol.condition = left;
            symbol.then = left;
            symbol.@else = parser.expression(0);
            return symbol;
        }
    }

    // match infix operators
    // <expression> <operator> <expression>
    // right associative
    internal abstract class InfixRFactory : NodeFactoryBase
    {
        internal InfixRFactory(string id, int bp)
            : base(id, bp)
        {
        }
    }

    //TODO: WTF??
    internal class InfixRErrorFactory : InfixRFactory
    {
        internal InfixRErrorFactory(string id, int bp) : base(id, bp)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            throw new NotSupportedException("TODO", null);
        }
    }

    internal class InfixRVariableBindFactory : InfixRFactory
    {
        internal InfixRVariableBindFactory(string id, int bp) : base(id, bp)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            Node symbol = new Node(token, SymbolType.binary);
            if (left.type != SymbolType.variable)
            {
                throw new JException("S0212", left.position, left.value);
            }
            symbol.lhs = left;
            symbol.rhs = parser.expression(Tokenizer.OPERATORS[":="] - 1); // subtract 1 from bindingPower for right associative operators
            return symbol;
        }
    }
}

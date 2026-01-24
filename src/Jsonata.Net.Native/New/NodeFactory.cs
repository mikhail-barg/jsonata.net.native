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
            return new Node(token);
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
            Node symbol = new Node(token);
            symbol.lhs = left;
            symbol.rhs = parser.expression(bp);
            symbol.type = SymbolType.binary;
            return symbol;
        }
    }

    internal sealed class InfixWithNudFactory : InfixFactory
    {
        public InfixWithNudFactory(string id) : base(id, 0)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            return new Node(token);
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
            Node symbol = new Node(token);
            symbol.type = this.m_symbolType;
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
            Node symbol = new Node(token);
            symbol.expression = parser.expression(70);
            symbol.type = SymbolType.unary;
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
            Node symbol = new Node(token);
            symbol.type = SymbolType.transform;
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
            Node symbol = new Node(token);
            symbol.type = SymbolType.descendant;
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
                Node term = new Node();
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
            Node symbol = new Node(token);
            symbol.lhs = left;
            symbol.rhsTerms = terms;
            symbol.type = SymbolType.binary;
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
            Node symbol = new Node(token);
            symbol.lhs = left;
            symbol.rhs = parser.expression(Tokenizer.OPERATORS["@"]);
            if (symbol.rhs.type != SymbolType.variable)
            {
                throw new JException("S0214", symbol.rhs.position, "@");
            }
            symbol.type = SymbolType.binary;
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
            Node symbol = new Node(token);
            symbol.lhs = left;
            symbol.rhs = parser.expression(Tokenizer.OPERATORS["#"]);
            if (symbol.rhs.type != SymbolType.variable)
            {
                throw new JException("S0214", symbol.rhs.position, "#");
            }
            symbol.type = SymbolType.binary;
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
            Node symbol = new Node(token);
            // left is is what we are trying to invoke
            symbol.procedure = left;
            symbol.type = SymbolType.function;
            symbol.arguments = new();
            if (parser.currentNodeFactory.id != ")")
            {
                while (true)
                {
                    if (parser.currentToken.type == SymbolType.@operator && parser.currentNodeFactory.id == "?")
                    {
                        // partial function application
                        symbol.type = SymbolType.partial;
                        //symbol.arguments.Add(parser.current_symbol); //TODO:convert to symbol!
                        symbol.arguments.Add(new Node() { value = "?", type = SymbolType.@operator });
                        parser.advance("?");
                    }
                    else
                    {
                        symbol.arguments.Add(parser.expression(0));
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
                foreach (Node arg in symbol.arguments)
                {
                    //this.arguments.forEach(function (arg, index) {
                    if (arg.type != SymbolType.variable)
                    {
                        throw new JException("S0208", arg.position, arg.value);
                    }
                    //index++;
                }
                symbol.type = SymbolType.lambda;
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
                    symbol.signature = new Signature(sig);
                }
                // parse the function body
                parser.advance("{");
                symbol.body = parser.expression(0);
                parser.advance("}");
            }
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
            Node symbol = new Node(token);
            symbol.type = SymbolType.block;
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
                        Node range = new Node();
                        range.type = SymbolType.binary;
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
            Node symbol = new Node(token);
            symbol.expressions = a;
            symbol.type = SymbolType.unary;
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
                Node symbol = new Node(token);
                symbol.lhs = left;
                symbol.rhs = parser.expression(Tokenizer.OPERATORS["]"]);
                symbol.type = SymbolType.binary;
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
            symbol.type = SymbolType.condition;
            Node c = new Node();
            symbol.condition = c;
            c.type = SymbolType.function;
            c.value = "(";
            c.arguments = new List<Node>() { left };
            Node p = new Node();
            c.procedure = p;
            p.type = SymbolType.variable;
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
            symbol.type = SymbolType.condition;
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
            symbol.type = SymbolType.condition;
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
            Node symbol = new Node(token);
            if (left.type != SymbolType.variable)
            {
                throw new JException("S0212", left.position, left.value);
            }
            symbol.lhs = left;
            symbol.rhs = parser.expression(Tokenizer.OPERATORS[":="] - 1); // subtract 1 from bindingPower for right associative operators
            symbol.type = SymbolType.binary;
            return symbol;
        }
    }
}

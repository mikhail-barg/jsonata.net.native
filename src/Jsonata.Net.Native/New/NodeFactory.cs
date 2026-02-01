using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Jsonata.Net.Native.Eval;

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

    // match infix operators
    // <expression> <operator> <expression>
    // left associative
    internal class InfixFactory : NodeFactoryBase
    {
        internal InfixFactory(string id)
            : base(id, Tokenizer.OPERATORS[id])
        {

        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            Node rhs = parser.expression(bp);
            Node result = new BinaryNode((string)token.value!, token.position, left, rhs);
            return result;
        }
    }

    internal sealed class InfixWithTypedNudFactory : InfixFactory
    {
        private readonly SymbolType m_symbolType;

        public InfixWithTypedNudFactory(string id, SymbolType symbolType)
            : base(id)
        {
            this.m_symbolType = symbolType;
        }

        internal override Node nud(Parser parser, Token token)
        {
            Node symbol = new Node(token, this.m_symbolType);
            return symbol;
        }
    }

    internal sealed class InfixWithOperatorPrefixFactory : InfixFactory
    {
        private readonly OperatorType m_operatorType;

        public InfixWithOperatorPrefixFactory(string id, OperatorType operatorType)
            : base(id)
        {
            this.m_operatorType = operatorType;
        }

        internal override Node nud(Parser parser, Token token)
        {
            Node result = new OperatorNode(token.position, this.m_operatorType);
            return result;
        }
    }

    internal class PrefixTransformerFactory : NodeFactoryBase
    {
        internal PrefixTransformerFactory(string id)
            : base(id)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            Node pattern = parser.expression(0);
            parser.advance("|");
            Node update = parser.expression(0);
            Node? delete;
            if (parser.currentNodeFactory.id == ",")
            {
                parser.advance(",");
                delete = parser.expression(0);
            }
            else
            {
                delete = null;
            }
            parser.advance("|");
            TransformNode result = new TransformNode(token.position, pattern, update, delete);
            return result;
        }
    }

    internal class PrefixDescendantWindcardFactory : NodeFactoryBase
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

    internal sealed class InfixAndPrefixMinusFactory : InfixFactory
    {
        internal InfixAndPrefixMinusFactory(string id)
            : base(id)
        {

        }

        internal override Node nud(Parser parser, Token token)
        {
            Node expression = parser.expression(70);
            Node result = new UnaryMinusNode(token.position, expression);
            return result;
        }
    }

    internal class InfixOrderByFactory : InfixFactory
    {
        internal InfixOrderByFactory(string id)
            : base(id)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            parser.advance("(");
            List<SortTermNode> terms = new();
            while (true)
            {
                bool descending = false;

                if (parser.currentNodeFactory.id == "<")
                {
                    // ascending sort
                    parser.advance("<");
                }
                else if (parser.currentNodeFactory.id == ">")
                {
                    // descending sort
                    descending = true;
                    parser.advance(">");
                }
                else
                {
                    //unspecified - default to ascending
                }
                Node expression = parser.expression(0);
                SortTermNode term = new SortTermNode(token.position, expression, descending);
                terms.Add(term);
                if (parser.currentNodeFactory.id != ",")
                {
                    break;
                }
                parser.advance(",");
            }
            parser.advance(")");
            OrderbyNode result = new OrderbyNode(token.position, lhs: left, rhsTerms: terms);
            return result;
        }
    }

    internal class InfixBlockFactory : InfixFactory
    {
        internal InfixBlockFactory(string id)
            : base(id)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            List<Tuple<Node, Node>> lhsObject = this.parseObject(parser);
            Node result = new GroupNode(token.position, lhsObject);
            return result;
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            List<Tuple<Node, Node>> rhsObject = this.parseObject(parser);
            Node result = new GroupByNode(token.position, left, rhsObject);
            return result;
        }

        private List<Tuple<Node, Node>> parseObject(Parser parser)
        {
            List<Tuple<Node, Node>> a = new();
            if (parser.currentNodeFactory.id != "}")
            {
                while (true)
                {
                    Node n = parser.expression(0);
                    parser.advance(":");
                    Node v = parser.expression(0);
                    Tuple<Node, Node> pair = Tuple.Create(n, v);
                    a.Add(pair); // holds an array of name/value expression pairs
                    if (parser.currentNodeFactory.id != ",")
                    {
                        break;
                    }
                    parser.advance(",");
                }
            }
            parser.advance("}", true);
            return a;
        }
    }

    internal class InfixBindContextVarFactory : InfixFactory
    {
        internal InfixBindContextVarFactory(string id)
            : base(id)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            Node rhs = parser.expression(Tokenizer.OPERATORS["@"]);
            if (rhs.type != SymbolType.variable)
            {
                throw new JException("S0214", rhs.position, "@");
            }
            Node result = new BindContextVarNode(token.position, left, (VariableNode)rhs);
            return result;
        }
    }

    internal class InfixBindPositionalVarFactory : InfixFactory
    {
        internal InfixBindPositionalVarFactory(string id)
            : base(id)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            Node rhs = parser.expression(Tokenizer.OPERATORS["#"]);
            if (rhs.type != SymbolType.variable)
            {
                throw new JException("S0214", rhs.position, "#");
            }
            Node result = new BindPositionalVarNode(token.position, left, (VariableNode)rhs);
            return result;
        }
    }

    internal class InfixInvocationFactory : InfixFactory
    {
        internal InfixInvocationFactory(string id)
            : base(id)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            SymbolType type = SymbolType.function;
            List<Node> arguments = new();
            if (parser.currentNodeFactory.id != ")")
            {
                while (true)
                {
                    if (parser.currentToken.type == SymbolType.@operator && parser.currentNodeFactory.id == "?")
                    {
                        // partial function application
                        type = SymbolType.partial;
                        //symbol.arguments.Add(parser.current_symbol); //TODO:convert to symbol!
                        arguments.Add(new OperatorNode(-1, OperatorType.partial));
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
            if (left.type == SymbolType.name
                && left is NameNode leftNameNode 
                && (leftNameNode.value == "function" || leftNameNode.value == "\u03BB")
            )
            {
                // all of the args must be VARIABLE tokens
                List<VariableNode> argVariables = new(arguments.Count);
                foreach (Node arg in arguments)
                {
                    if (arg.type != SymbolType.variable)
                    {
                        throw new JException("S0208", arg.position/*, arg.value*/); //TODO: value
                    }
                    argVariables.Add((VariableNode)arg);
                }
                // type = SymbolType.lambda;
                Signature? signature = null;
                // is the next token a '<' - if so, parse the function signature
                if (parser.currentNodeFactory.id == "<")
                {
                    int depth = 1;
                    String sig = "<";
                    while (depth > 0 && parser.currentNodeFactory.id != "{" && parser.currentNodeFactory != Parser.s_terminalFactoryEnd)
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
                Node body = parser.expression(0);
                parser.advance("}");

                Node result = new LambdaNode(token.position, arguments: argVariables, signature: signature, body: body, thunk: false);
                return result;
            }
            else
            {
                // left is is what we are trying to invoke
                Node result = new FunctionalNode(type, token.position, procedure: left, arguments: arguments);
                return result;
            }
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
            BlockNode result = new BlockNode(token.position, expressions);
            return result;
        }
    }

    internal class InfixArrayFactory : InfixFactory
    {
        internal InfixArrayFactory(string id)
            : base(id)
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
                        Node lhs = item;
                        int pos = parser.currentToken.position;
                        parser.advance("..");
                        Node rhs = parser.expression(0);
                        Node range = new BinaryNode("..", pos, lhs, rhs);
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
            Node result = new ArrayNode(token.position, a);
            return result;
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            if (parser.currentNodeFactory.id == "]")
            {
                // empty predicate means maintain singleton arrays in the output
                Node? step = left;
                while (step is BinaryNode binaryStep && binaryStep.value.Equals("["))
                {
                    step = binaryStep.lhs;
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
                Node rhs = parser.expression(Tokenizer.OPERATORS["]"]);
                Node symbol = new BinaryNode((string)token.value!, token.position, left, rhs);
                parser.advance("]", true);
                return symbol;
            }
        }
    }

    internal class InfixCoalescingFactory : InfixFactory
    {
        internal InfixCoalescingFactory(string id)
            : base(id)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            Node procedure = new VariableNode(-1, nameof(BuiltinFunctions.exists));     //TODO: probably should be 'name'??
            FunctionalNode condition = new FunctionalNode(SymbolType.function, -1, procedure: procedure, arguments: new() { left });
            Node @else = parser.expression(0);
            ConditionNode result = new ConditionNode(token.position, condition: condition, then: left, @else: @else);
            return result;
        }
    }

    internal class InfixTernaryFactory : InfixFactory
    {
        internal InfixTernaryFactory(string id)
            : base(id)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            Node then = parser.expression(0);
            Node? @else;
            if (parser.currentNodeFactory.id == ":")
            {
                // else condition
                parser.advance(":");
                @else = parser.expression(0);
            }
            else
            {
                @else = null;
            }
            ConditionNode result = new ConditionNode(token.position, condition: left, then: then, @else: @else);
            return result;
        }
    }

    internal class InfixElvisFactory : InfixFactory
    {
        internal InfixElvisFactory(string id)
            : base(id)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            Node @else = parser.expression(0);
            ConditionNode result = new ConditionNode(token.position, condition: left, then: left, @else: @else);
            return result;
        }
    }

    internal class InfixBindAssignVarFactory : InfixFactory
    {
        internal InfixBindAssignVarFactory(string id)
            : base(id)
        {
        }

        internal override Node led(Node left, Parser parser, Token token)
        {
            if (left.type != SymbolType.variable)
            {
                throw new JException("S0212", left.position/*, left.value*/); //TODO: value
            }
            Node rhs = parser.expression(Tokenizer.OPERATORS[":="] - 1); // subtract 1 from bindingPower for right associative operators
            Node result = new BindAssignVarNode(token.position, (VariableNode)left, rhs);
            return result;
        }
    }

    internal sealed class TerminalFactoryNumberInt : NodeFactoryBase
    {
        public TerminalFactoryNumberInt() : base($"(int)", 0)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            if (token.type != SymbolType._number_int)
            {
                throw new Exception($"Should not happen: got {token.type}, expected {SymbolType._number_int}");
            }
            return new NumberIntNode(token.position, (long)token.value!);
        }
    }

    internal sealed class TerminalFactoryNumberDouble : NodeFactoryBase
    {
        public TerminalFactoryNumberDouble() : base($"(double)", 0)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            if (token.type != SymbolType._number_double)
            {
                throw new Exception($"Should not happen: got {token.type}, expected {SymbolType._number_double}");
            }
            return new NumberDoubleNode(token.position, (double)token.value!);
        }
    }

    internal sealed class TerminalFactoryString : NodeFactoryBase
    {
        public TerminalFactoryString() : base($"(string)", 0)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            if (token.type != SymbolType.@string)
            {
                throw new Exception($"Should not happen: got {token.type}, expected {SymbolType.@string}");
            }
            return new StringNode(token.position, (string)token.value!);
        }
    }

    internal sealed class TerminalFactoryValueBool : NodeFactoryBase
    {
        public TerminalFactoryValueBool() : base($"(bool)", 0)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            if (token.type != SymbolType._value_bool)
            {
                throw new Exception($"Should not happen: got {token.type}, expected {SymbolType._value_bool}");
            }
            return new ValueBoolNode(token.position, (bool)token.value!);
        }
    }

    internal sealed class TerminalFactoryValueNull : NodeFactoryBase
    {
        public TerminalFactoryValueNull() : base($"(null)", 0)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            if (token.type != SymbolType._value_null)
            {
                throw new Exception($"Should not happen: got {token.type}, expected {SymbolType._value_null}");
            }
            return new ValueNullNode(token.position);
        }
    }

    internal sealed class TerminalFactoryVariable : NodeFactoryBase
    {
        public TerminalFactoryVariable() : base($"(var)", 0)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            if (token.type != SymbolType.variable)
            {
                throw new Exception($"Should not happen: got {token.type}, expected {SymbolType.variable}");
            }
            return new VariableNode(token.position, (string)token.value!);
        }
    }

    internal sealed class TerminalFactoryName : NodeFactoryBase
    {
        public TerminalFactoryName() : base($"(name)", 0)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            if (token.type != SymbolType.name)
            {
                throw new Exception($"Should not happen: got {token.type}, expected {SymbolType.name}");
            }
            return new NameNode(token.position, (string)token.value!);
        }
    }

    internal sealed class TerminalFactoryRegex : NodeFactoryBase
    {
        public TerminalFactoryRegex() : base($"(regex)", 0)
        {
        }

        internal override Node nud(Parser parser, Token token)
        {
            if (token.type != SymbolType.regex)
            {
                throw new Exception($"Should not happen: got {token.type}, expected {SymbolType.regex}");
            }
            return new RegexNode(token.position, (Regex)token.value!);
        }
    }
    internal sealed class TerminalFactoryTyped : NodeFactoryBase
    {
        private readonly SymbolType m_type;

        public TerminalFactoryTyped(SymbolType type) : base($"({type})", 0)
        {
            this.m_type = type;
        }

        internal override Node nud(Parser parser, Token token)
        {
            if (token.type != this.m_type)
            {
                throw new Exception($"Should not happen: got {token.type}, expected {this.m_type}");
            }
            return new Node(token, this.m_type);
        }
    }
}
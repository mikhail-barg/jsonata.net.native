using System;
using System.Collections.Generic;
using System.Linq;

namespace Jsonata.Net.Native.New
{ 
    internal sealed class Parser 
    {
        // This parser implements the 'Top down operator precedence' algorithm developed by Vaughan R Pratt; http://dl.acm.org/citation.cfm?id=512931.
        // and builds on the Javascript framework described by Douglas Crockford at http://javascript.crockford.com/tdop/tdop.html
        // and in 'Beautiful Code', edited by Andy Oram and Greg Wilson, Copyright 2007 O'Reilly Media, Inc. 798-0-596-51004-6

        private readonly string source;
        private readonly Tokenizer lexer;

        //var parser = function (source, recover) {
        internal NodeFactoryBase currentNodeFactory { get; private set; } = default!;
        internal Token currentToken { get; private set; } = default!;

        private Parser(string source)
        {
            this.source = source;

            // now invoke the tokenizer and the parser and return the syntax tree
            this.lexer = new Tokenizer(this.source);
            this.advance();
        }

        internal void advance() 
        { 
            this.advance(null); 
        }
        
        internal void advance(string? id) 
        { 
            this.advance(id, false); 
        }

        internal void advance(string? id, bool infix) 
        {
            if (id != null && this.currentNodeFactory.id != id) 
            {
                String code;
                if (this.currentNodeFactory == Parser.s_terminalFactoryEnd) 
                {
                    // unexpected end of buffer
                    code = "S0203";
                } 
                else 
                {
                    code = "S0202";
                }
                throw new JException(
                    code,
                    this.currentToken.position,
                    id,
                    this.currentToken.value
                );
            }
            Token? next_token = lexer.next(infix);
            NodeFactoryBase factory;
            if (next_token == null) 
            {
                this.currentNodeFactory = s_terminalFactoryEnd;
                this.currentToken = new Token(SymbolType._end, null, source.Length);
                return;
            }
            this.currentToken = next_token;
            switch (this.currentToken.type) 
            {
            case SymbolType.name:
                factory = Parser.s_terminalFactoryName;
                break;
            case SymbolType.variable:
                factory = Parser.s_terminalFactoryVariable;
                break;
            case SymbolType.@operator:
                if (!Parser.s_binaryFactoryTable.TryGetValue(this.currentToken.value!.ToString()!, out NodeFactoryBase? foundFactory))
                {
                    throw new JException("S0204", this.currentToken.position, this.currentToken.value);
                }
                else
                {
                    factory = foundFactory;
                }
                break;
            case SymbolType.@string:
                factory = Parser.s_terminalFactoryString;
                break;
            case SymbolType._number_double:
                factory = Parser.s_terminalFactoryNumberDouble;
                break;
            case SymbolType._number_int:
                factory = Parser.s_terminalFactoryNumberInt;
                break;
            case SymbolType._value_bool:
                factory = Parser.s_terminalFactoryValueBool;
                break;
            case SymbolType._value_null:
                factory = Parser.s_terminalFactoryValueNull;
                break;
            case SymbolType.regex:
                factory = Parser.s_terminalFactoryRegex;
                break;
            default:
                throw new JException("S0205", this.currentToken.position, this.currentToken.value);
            }

            this.currentNodeFactory = factory;
        }

        // Pratt's algorithm
        internal Node expression(int rbp) 
        {
            Node left;
            NodeFactoryBase f = this.currentNodeFactory;
            Token t = this.currentToken;
            this.advance(null, true);
            left = f.nud(this, t);
            while (rbp < this.currentNodeFactory.bp) //was LBP
            {
                f = this.currentNodeFactory;
                t = this.currentToken;
                advance(null, false);
                left = f.led(left, this, t);
            }
            return left;
        }

        internal static Node Parse(string jsonata) 
        {
            Parser parser = new Parser(jsonata);
            // parse the tokens
            Node expr = parser.expression(0);
            if (parser.currentNodeFactory != Parser.s_terminalFactoryEnd) 
            {
                throw new JException("S0201", parser.currentToken.position, parser.currentToken.value);
            }

            Node result = Optimizer.OptimizeAst(expr);
            return result;
        }

        private static readonly Dictionary<string, NodeFactoryBase> s_binaryFactoryTable = CreateNodeFactoryTable();
        internal static readonly NodeFactoryBase s_terminalFactoryEnd = new TerminalFactoryEnd();
        internal static readonly NodeFactoryBase s_terminalFactoryName = new TerminalFactoryName();
        internal static readonly NodeFactoryBase s_terminalFactoryVariable = new TerminalFactoryVariable();
        internal static readonly NodeFactoryBase s_terminalFactoryNumberDouble = new TerminalFactoryNumberDouble();
        internal static readonly NodeFactoryBase s_terminalFactoryNumberInt = new TerminalFactoryNumberInt();
        internal static readonly NodeFactoryBase s_terminalFactoryString = new TerminalFactoryString();
        internal static readonly NodeFactoryBase s_terminalFactoryValueBool = new TerminalFactoryValueBool();
        internal static readonly NodeFactoryBase s_terminalFactoryValueNull = new TerminalFactoryValueNull();
        internal static readonly NodeFactoryBase s_terminalFactoryRegex = new TerminalFactoryRegex();

        private static void register(Dictionary<string, NodeFactoryBase> nodeFactoryTable, NodeFactoryBase t)
        {
            if (nodeFactoryTable.TryGetValue(t.id, out NodeFactoryBase? s))
            {
                throw new Exception("Handle combine?? " + t.id);
            }
            else
            {
                nodeFactoryTable.Add(t.id, t);
            }
        }

        private static Dictionary<string, NodeFactoryBase> CreateNodeFactoryTable() 
        {
            Dictionary<string, NodeFactoryBase> nodeFactoryTable = new();
            register(nodeFactoryTable, new DummyNodeFactory(":"));
            register(nodeFactoryTable, new DummyNodeFactory(";"));
            register(nodeFactoryTable, new DummyNodeFactory(","));
            register(nodeFactoryTable, new DummyNodeFactory(")"));
            register(nodeFactoryTable, new DummyNodeFactory("]"));
            register(nodeFactoryTable, new DummyNodeFactory("}"));
            register(nodeFactoryTable, new DummyNodeFactory("..")); // range operator
            register(nodeFactoryTable, new InfixMapFactory(".")); // map operator
            register(nodeFactoryTable, new InfixFactory("+")); // numeric addition
            register(nodeFactoryTable, new InfixAndPrefixMinusFactory("-")); // numeric subtraction // unary numeric negation

            register(nodeFactoryTable, new InfixWildcardFactory("*")); // field wildcard (single level) // numeric multiplication
            register(nodeFactoryTable, new InfixFactory("/")); // numeric division
            register(nodeFactoryTable, new InfixParentFactory("%")); // parent operator // numeric modulus
            register(nodeFactoryTable, new InfixFactory("=")); // equality
            register(nodeFactoryTable, new InfixFactory("<")); // less than
            register(nodeFactoryTable, new InfixFactory(">")); // greater than
            register(nodeFactoryTable, new InfixFactory("!=")); // not equal to
            register(nodeFactoryTable, new InfixFactory("<=")); // less than or equal
            register(nodeFactoryTable, new InfixFactory(">=")); // greater than or equal
            register(nodeFactoryTable, new InfixFactory("&")); // string concatenation

            register(nodeFactoryTable, new InfixWithOperatorPrefixFactory("and", SpecialOperatorType.and)); // allow as terminal // Boolean AND
            register(nodeFactoryTable, new InfixWithOperatorPrefixFactory("or", SpecialOperatorType.or)); // allow as terminal // Boolean OR
            register(nodeFactoryTable, new InfixWithOperatorPrefixFactory("in", SpecialOperatorType.@in)); // allow as terminal // is member of array
            register(nodeFactoryTable, new InfixApplyFactory("~>")); // function application
            register(nodeFactoryTable, new InfixCoalescingFactory("??"));   // coalescing operator
            register(nodeFactoryTable, new PrefixDescendantWindcardFactory("**")); // descendant wildcard (multi-level)
            register(nodeFactoryTable, new InfixInvocationFactory("(")); // function invocation
            register(nodeFactoryTable, new InfixArrayFactory("[")); // array constructor // merged: register(new Prefix("[") {        
            register(nodeFactoryTable, new InfixOrderByFactory("^")); // order-by
            register(nodeFactoryTable, new InfixBlockFactory("{"));
            register(nodeFactoryTable, new InfixBindAssignVarFactory(":=")); // bind variable
            register(nodeFactoryTable, new InfixBindContextVarFactory("@")); // focus variable bind
            register(nodeFactoryTable, new InfixBindPositionalVarFactory("#")); // index (position) variable bind
            register(nodeFactoryTable, new InfixTernaryFactory("?")); // if/then/else ternary operator ?:
            register(nodeFactoryTable, new InfixElvisFactory("?:")); // elvis/default operator
            register(nodeFactoryTable, new PrefixTransformerFactory("|")); // object transformer
            return nodeFactoryTable;
        }
    }
}
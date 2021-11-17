using Jsonata.Net.Native.Parsing;
using System;
using System.Collections.Generic;

namespace Jsonata.Net.Native.Parsing
{
    internal sealed partial class Parser
    {
        private readonly Lexer m_lexer;
        private Token token = new Token(TokenType.typeError, null, -1);
        private readonly Dictionary<TokenType, Nud> m_nuds;
        private readonly Dictionary<TokenType, Led> m_leds;
        private readonly Dictionary<TokenType, int> m_bps;

        internal static Node Parse(string queryText)
        {
            Parser parser = new Parser(queryText);
            Node result = parser.parseExpression(0);
            if (parser.token.type != TokenType.typeEOF) 
            {
                throw new ErrSyntaxError(parser.token);
            }
            return result.optimize();
        }

        private Parser(string queryText)
        {
            // nuds defines nud functions for token types that are valid
            // in the prefix position.
            this.m_nuds = new Dictionary<TokenType, Nud>() {
                { TokenType.typeString,      this.parseString },
                { TokenType.typeNumber,      this.parseNumber },
                { TokenType.typeBoolean,     this.parseBoolean },
                { TokenType.typeNull,        this.parseNull },
                { TokenType.typeRegex,       this.parseRegex },
                { TokenType.typeVariable,    this.parseVariable },
                { TokenType.typeName,        this.parseName },
                { TokenType.typeNameEsc,     this.parseEscapedName },
                { TokenType.typeBracketOpen, this.parseArray },
                { TokenType.typeBraceOpen,   this.parseObject },
                { TokenType.typeParenOpen,   this.parseBlock },
                { TokenType.typeMult,        this.parseWildcard },
                { TokenType.typeMinus,       this.parseNegation },
                { TokenType.typeDescendent,  this.parseDescendent },
                { TokenType.typePipe,        this.parseObjectTransformation },
                { TokenType.typeIn,          this.parseName },
                { TokenType.typeAnd,         this.parseName },
                { TokenType.typeOr,          this.parseName },
            };

            this.m_leds = new Dictionary<TokenType, Led>() {
                { TokenType.typeParenOpen,    this.parseFunctionCall },
                { TokenType.typeBracketOpen,  this.parsePredicate },
                { TokenType.typeBraceOpen,    this.parseGroup },
                { TokenType.typeCondition,    this.parseConditional },
                { TokenType.typeAssign,       this.parseAssignment },
                { TokenType.typeApply,        this.parseFunctionApplication },
                { TokenType.typeConcat,       this.parseStringConcatenation },
                { TokenType.typeSort,         this.parseSort },
                { TokenType.typeDot,          this.parseDot },
                { TokenType.typePlus,         this.parseNumericOperator },
                { TokenType.typeMinus,        this.parseNumericOperator },
                { TokenType.typeMult,         this.parseNumericOperator },
                { TokenType.typeDiv,          this.parseNumericOperator },
                { TokenType.typeMod,          this.parseNumericOperator },
                { TokenType.typeEqual,        this.parseComparisonOperator },
                { TokenType.typeNotEqual,     this.parseComparisonOperator },
                { TokenType.typeLess,         this.parseComparisonOperator },
                { TokenType.typeLessEqual,    this.parseComparisonOperator },
                { TokenType.typeGreater,      this.parseComparisonOperator },
                { TokenType.typeGreaterEqual, this.parseComparisonOperator },
                { TokenType.typeIn,           this.parseComparisonOperator },
                { TokenType.typeAnd,          this.parseBooleanOperator },
                { TokenType.typeOr,           this.parseBooleanOperator },
            };
            
            this.m_bps = InitBindingPowers();
            ValidateBindingPowers(this.m_bps, this.m_leds);

            this.m_lexer = new Lexer(queryText);
            // Set current token to the first token in the expression.
            this.advance(true);
        }


        // bps defines binding powers for token types that are valid
        // in the infix position. The parsing algorithm requires that
        // all infix operators (as defined by the leds variable above)
        // have a non-zero binding power.
        //
        // Binding powers are calculated from a 2D slice of token types
        // in which the outer slice is ordered by operator precedence
        // (highest to lowest) and each inner slice contains token
        // types of equal operator precedence.
        //
        // initBindingPowers calculates binding power values for the
        // given token types and returns them as an array. The specific
        // values are not important. All that matters for parsing is
        // whether one token's binding power is higher than another's.
        private static Dictionary<TokenType, int> InitBindingPowers()
        {
            Dictionary<TokenType, int> results = new Dictionary<TokenType, int>();

            // Binding powers must:
            //
            //   1. be non-zero
            //   2. increase with operator precedence
            //   3. be separated by more than one (because we subtract
            //      1 from the binding power for right-associative
            //      operators).
            //
            // This function produces a minimum binding power of 10.
            // Values increase by 10 as operator precedence increases.

            // Token types are provided as a slice of slices. The outer
            // slice is ordered by operator precedence, highest to lowest.
            // Token types within each inner slice have the same operator
            // precedence.
            TokenType[][] tokenTypes = new TokenType[][] {
                new TokenType[] { TokenType.typeParenOpen, TokenType.typeBracketOpen, },
                new TokenType[] { TokenType.typeDot, },
                new TokenType[] { TokenType.typeBraceOpen, },
                new TokenType[] { TokenType.typeMult, TokenType.typeDiv, TokenType.typeMod, },
                new TokenType[] { TokenType.typePlus, TokenType.typeMinus, TokenType.typeConcat, },
                new TokenType[] { TokenType.typeEqual, TokenType.typeNotEqual, TokenType.typeLess, TokenType.typeLessEqual, TokenType.typeGreater, TokenType.typeGreaterEqual, 
                                    TokenType.typeIn, TokenType.typeSort, TokenType.typeApply,},
                new TokenType[] { TokenType.typeAnd, },
                new TokenType[] { TokenType.typeOr, },
                new TokenType[] { TokenType.typeCondition, },
                new TokenType[] { TokenType.typeAssign, },
            };

            for (int offset = 0; offset < tokenTypes.Length; ++offset)
            {
                TokenType[] tts = tokenTypes[offset];
                int bp = (tokenTypes.Length - offset) * 10;
                foreach (TokenType tt in tts)
                {
                    results.Add(tt, bp);
                }
            }

            foreach (TokenType tt in Enum.GetValues(typeof(TokenType)))
            {
                if (!results.ContainsKey(tt))
                {
                    results.Add(tt, 0);
                }
            }

            return results;
        }

        // validateBindingPowers sanity checks the values calculated
        // by initBindingPowers. Every token type in the leds array
        // should have a binding power. No other token type should
        // have a binding power.
        private static void ValidateBindingPowers(Dictionary<TokenType, int> bps, Dictionary<TokenType, Led> leds)
        {
            foreach (TokenType tt in Enum.GetValues(typeof(TokenType)))
            {
                if (leds.ContainsKey(tt) && bps[tt] == 0)
                {
                    throw new Exception($"validateBindingPowers: token type {tt} does not have a binding power");
                }
                else if (!leds.ContainsKey(tt) && bps[tt] != 0)
                {
                    throw new Exception($"validateBindingPowers: token type {tt} should not have a binding power");
                }
            }
        }


        // advance requests the next token from the lexer and updates
        // the parser's current token pointer. It panics if the lexer
        // returns an error token.
        private void advance(bool allowRegex)
        {
            this.token = this.m_lexer.next(allowRegex);
            if (this.token.type == TokenType.typeError)
            {
                throw new Exception();
            }
        }

        // consume is like advance except it first checks that the
        // current token is of the expected type. It panics if that
        // is not the case.
        private void consume(TokenType expected, bool allowRegex)
        {
            if (this.token.type != expected)
            {
                if (this.token.type == TokenType.typeEOF)
                {
                    throw new JsonataException("S0203", $"expected token '{Lexer.TokenTypeToString(expected)}' ({expected}) before end of expression");
                };
                throw new ErrUnexpectedToken(expected, this.token);
            }

            this.advance(allowRegex);
        }

        // parseExpression is the central function of the Pratt
        // algorithm. It handles dispatch to the various nud/led
        // functions (which may call back into parseExpression
        // and the other parser methods).
        private Node parseExpression(int rbp)
        {
            if (this.token.type == TokenType.typeEOF)
            {
                throw new ErrUnexpectedEOF(this.token);
            }
            Token t = this.token;
            this.advance(false);

            if (!this.m_nuds.TryGetValue(t.type, out Nud? nud))
            {
                throw new ErrPrefix(t);
            };
            Node lhs = nud(t);
            while (rbp < this.m_bps[this.token.type])
            {
                t = this.token;
                this.advance(true);

                if (!this.m_leds.TryGetValue(t.type, out Led? led))
                {
                    throw new ErrInfix(t);
                }
                lhs = led(t, lhs);
            }
            return lhs;
        }

        // The JSONata parser is based on Pratt's Top Down Operator
        // Precededence algorithm (see https://tdop.github.io/). Given
        // a series of tokens representing a JSONata expression and the
        // following metadata, it converts the tokens into an abstract
        // syntax tree:
        //
        // 1. Functions that convert tokens to nodes based on their
        //    type and position (see 'nud' and 'led' in Pratt).
        //
        // 2. Binding powers (i.e. operator precedence values) for
        //    infix operators (see 'lbp' in Pratt).
        //
        // This metadata is defined below.

        // A nud (short for null denotation) is a function that takes
        // a token and returns a node representing that token's value.
        // The parsing algorithm only calls the nud function for tokens
        // in the prefix position. This includes simple values like
        // strings and numbers, complex values like arrays and objects,
        // and prefix operators like the negation operator.
        delegate Node Nud(Token token);

        // An led (short for left denotation) is a function that takes
        // a token and a node representing the left hand side of an
        // infix operation, and returns a node representing that infix
        // operation. The parsing algorithm only calls the led function
        // for tokens in the infix position, e.g. the mathematical
        // operators.
        delegate Node Led(Token token, Node node);
    }
}
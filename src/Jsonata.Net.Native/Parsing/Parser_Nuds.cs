using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jsonata.Net.Native.Dom;

namespace Jsonata.Net.Native.Parsing
{
    internal sealed partial class Parser
    {

        private Node parseString(Token t)
        {
            string s = Helpers.Unescape(t.value!);
            return new StringNode(value: s);
        }

        private Node parseNumber(Token t)
        {
            if (Int64.TryParse(t.value!, out long longValue))
            {
                return new NumberIntNode(longValue);
            }
            else if (Double.TryParse(t.value!, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue))
            {
                longValue = (long)doubleValue;
                if (longValue == doubleValue)
                {
                    //try to simplify double, for example 1e7 is expected to be int
                    return new NumberIntNode(longValue);
                }
                else
                {
                    return new NumberDoubleNode(doubleValue);
                }
            }
            else
            {
                throw new JsonataException("S0102", $"Number out of range: '{t.value}'"); //TODO: is it a correct code?
            }
        }

        private Node parseBoolean(Token t)
        {
            bool b;
            switch (t.value)
            {
            case "true":
                b = true;
                break;
            case "false":
                b = false;
                break;
            default: // should be unreachable
                throw new JsonataException("????", $"invalid bool literal '{token.value}'");
            };

            return new BooleanNode(b);
        }

        private Node parseNull(Token t)
        {
            return new NullNode();
        }

        private Node parseRegex(Token t)
        {
            RegexToken regexToken = (RegexToken)t;

            if (string.IsNullOrWhiteSpace(regexToken.value)) 
            {
                //TODO:
                throw new JsonataException("????", "Empty regex");
            }

            Regex regex;
            try
            {
                regex = new Regex(regexToken.value, regexToken.flags);
            }
            catch (Exception e)
            {
                //TODO:
                throw new JsonataException("????", "Invalid regex: " + e.Message);
            }

            return new RegexNode(regex, regexToken.value!);
        }

        private Node parseVariable(Token t)
        {
            return new VariableNode(name: t.value ?? throw new ArgumentException());
        }

        private Node parseName(Token t)
        {
            return new NameNode(value: t.value ?? throw new ArgumentException(), escaped: false);
        }

        private Node parseParent(Token t)
        {
            return new ParentNode();
        }

        private Node parseEscapedName(Token t)
        {
            return new NameNode(value: t.value ?? throw new ArgumentException(), escaped: true);
        }

        private Node parseNegation(Token t)
        {
            return new NegationNode(rhs: this.parseExpression(this.m_bps[t.type]));
        }

        private Node parseArray(Token t)
        {
            List<Node> items = new List<Node>();

            while (this.token.type != TokenType.typeBracketClose)
            {
                // disallow trailing commas
                Node item = this.parseExpression(0);
                if (this.token.type == TokenType.typeRange)
                {
                    this.consume(TokenType.typeRange, true);
                    item = new RangeNode(
                        lhs: item,
                        rhs: this.parseExpression(0)
                    );
                }
                items.Add(item);

                if (this.token.type != TokenType.typeComma)
                {
                    break;
                }
                this.consume(TokenType.typeComma, true);
            }

            this.consume(TokenType.typeBracketClose, false);

            return new ArrayNode(items);
        }


        private ObjectNode parseObject(Token t)
        {
            List<Tuple<Node, Node>> pairs = new List<Tuple<Node, Node>>();
            while (this.token.type != TokenType.typeBraceClose)  // TODO: disallow trailing commas
            {
                Node key = this.parseExpression(0);
                this.consume(TokenType.typeColon, true);
                Node value = this.parseExpression(0);
                pairs.Add(Tuple.Create(key, value));
                if (this.token.type != TokenType.typeComma)
                {
                    break;
                }
                this.consume(TokenType.typeComma, true);
            }
            this.consume(TokenType.typeBraceClose, false);
            return new ObjectNode(pairs);
        }

        private Node parseBlock(Token t)
        {
            List<Node> exprs = new List<Node>();
            while (this.token.type != TokenType.typeParenClose)  // allow trailing semicolons
            {
                Node expr = this.parseExpression(0);
                exprs.Add(expr);
                if (this.token.type != TokenType.typeSemicolon)
                {
                    break;
                }
                this.consume(TokenType.typeSemicolon, true);
            }
            this.consume(TokenType.typeParenClose, false);
            return new BlockNode(exprs);
        }

        private Node parseWildcard(Token t)
        {
            return new WildcardNode();
        }

        private Node parseDescendant(Token t)
        {
            return new DescendantNode();
        }

        private Node parseObjectTransformation(Token t)
        {
            Node? deletes = null;
            Node pattern = this.parseExpression(0);
            this.consume(TokenType.typePipe, true);
            Node updates = this.parseExpression(0);
            if (this.token.type == TokenType.typeComma)
            {
                this.consume(TokenType.typeComma, true);
                deletes = this.parseExpression(0);
            }
            this.consume(TokenType.typePipe, true);

            return new ObjectTransformationNode(pattern, updates, deletes);
        }
    }
}

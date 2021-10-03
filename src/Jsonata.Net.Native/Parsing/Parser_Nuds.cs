using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            else if (Double.TryParse(t.value!, out double doubleValue))
            {
                return new NumberDoubleNode(doubleValue);
            }
            else
            {
                throw new ErrInvalidNumber(t);
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
                throw new ErrInvalidBool(t);
            };

            return new BooleanNode(b);
        }

        private Node parseNull(Token t)
        {
            return new NullNode();
        }

        private Node parseRegex(Token t)
        {
            //TODO: implement!
            throw new NotImplementedException();
            /*
	        if (t.Value == "") 
            {
		        return nil, newError(ErrEmptyRegex, t)
            }

            re, err := regexp.Compile(t.Value)
	        if (err != nil) 
            {
		        hint := "unknown error"
		        if e, ok := err.(* syntax.Error); ok {
			        hint = string (e.Code)
                }
                return nil, newErrorHint(ErrInvalidRegex, t, hint)
	        }

	        return &RegexNode{Value: re}, nil
            */
        }

        private Node parseVariable(Token t)
        {
            return new VariableNode(name: t.value ?? throw new ArgumentException());
        }

        private Node parseName(Token t)
        {
            return new NameNode(value: t.value ?? throw new ArgumentException(), escaped: false);
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


        private Node parseObject(Token t)
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

        private Node parseDescendent(Token t)
        {
            return new DescendentNode();
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

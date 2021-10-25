using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Parsing
{
    internal sealed partial class Parser
    {
        private Node parseFunctionCall(Token t, Node lhs)
        {
            (bool isLambda, bool isShorthand) = isLambdaName(lhs);
            if (isLambda)
            {
                return this.ParseLambdaDefinition(isShorthand);
            };

            const TokenType placeholderTokenType = TokenType.typeCondition;

            bool isPartial = false;
            List<Node> args = new List<Node>();
            while (this.token.type != TokenType.typeParenClose) //TODO: disallow trailing commas
            {
                Node arg;
                if (this.token.type == placeholderTokenType)
                {
                    isPartial = true;
                    arg = new PlaceholderNode();
                    this.consume(placeholderTokenType, true);
                }
                else
                {
                    arg = this.parseExpression(0);
                };
                args.Add(arg);
                if (this.token.type != TokenType.typeComma)
                {
                    break;
                }
                this.consume(TokenType.typeComma, true);
            }
            this.consume(TokenType.typeParenClose, false);
            if (isPartial)
            {
                return new PartialNode(lhs, args);
            }
            else
            {
                return new FunctionCallNode(lhs, args);
            }
        }

        private Node ParseLambdaDefinition(bool isShorthand)
        {
            List<string> paramNames = this.extractParamNames();
            string? signatureString = this.extractSignature();
            LambdaNode.Signature? signature = signatureString != null ? SignatureParser.Parse(signatureString) : null;
            this.consume(TokenType.typeBraceOpen, true);
            Node body = this.parseExpression(0);
            this.consume(TokenType.typeBraceClose, true);
            return new LambdaNode(isShorthand, paramNames, signature, body);
        }

        
        private string? extractSignature()
        {
            const TokenType typeSigStart = TokenType.typeLess;
            const TokenType typeSigEnd = TokenType.typeGreater;

            if (this.token.type != typeSigStart)
            {
                return null;
            }

            //TODO use substring instaed of string builder;
            StringBuilder builder = new StringBuilder();
            int depth = 1;
            builder.Append(this.token.value);
            while (this.token.type != TokenType.typeBraceOpen && this.token.type != TokenType.typeEOF)
            {
                this.advance(true);
                builder.Append(this.token.value);
                if (this.token.type == typeSigEnd)
                {
                    --depth;
                    if (depth == 0)
                    {
                        break;
                    }
                }
                else if (this.token.type == typeSigStart)
                {
                    ++depth;
                };
            }
            this.consume(typeSigEnd, true);
            return builder.ToString();
        }

        private List<string> extractParamNames()
        {
            List<string> names = new List<string>();
            Token currToken = this.token;
            while (this.token.type != TokenType.typeParenClose) // TODO: disallow trailing commas
            {
                Node arg = this.parseExpression(0);
                if (arg is not VariableNode variable)
                {
                    //TODO: use proper code
                    throw new JsonataException("????", $"Function argument declaration should be a variable name (starts with $), got {arg}");
                };
                if (names.Contains(variable.name))
                {
                    //TODO: use proper code
                    throw new JsonataException("????", $"Function argument declaration should be a variable name (starts with $), got {arg}");
                };
                names.Add(variable.name);
                if (this.token.type != TokenType.typeComma)
                {
                    break;
                }
                this.consume(TokenType.typeComma, true);
            }
            this.consume(TokenType.typeParenClose, false);
            return names;
        }

        private static (bool isLambda, bool isShorthand) isLambdaName(Node node)
        {
            if (node is NameNode nameNode)
            {
                switch (nameNode.value)
                {
                case "function":
                    return (true, false);
                case "λ":
                    return (true, true);
                }
            }
            return (false, false);
        }

        private Node parsePredicate(Token t, Node lhs)
        {
            if (this.token.type == TokenType.typeBracketClose) 
            {
                this.consume(TokenType.typeBracketClose, false);
                // Empty brackets in a path mean that we should not
                // flatten singleton arrays into single values.
                return new SingletonArrayNode_(lhs: lhs);
            };

            Node rhs = this.parseExpression(0);

            this.consume(TokenType.typeBracketClose, false);
            return new PredicateNode_(lhs: lhs, rhs: rhs);
        }

        private Node parseGroup(Token t, Node lhs)
        {
            ObjectNode objectNode = this.parseObject(t);
            return new GroupNode(lhs, objectNode);
        }

        private Node parseConditional(Token t, Node lhs)
        {
            Node expr1 = this.parseExpression(0);
            Node? expr2;
            if (this.token.type == TokenType.typeColon)
            {
                this.consume(TokenType.typeColon, true);
                expr2 = this.parseExpression(0);
            }
            else
            {
                expr2 = null;
            };
            return new ConditionalNode(lhs, expr1, expr2);
        }

        private Node parseAssignment(Token t, Node lhs)
        {
            if (lhs is not VariableNode variableNode)
            {
                throw new JsonataException("S0212", "The left side of := must be a variable name (start with $)");
            };
            return new AssignmentNode(
                name: variableNode.name,
                value: this.parseExpression(this.m_bps[t.type] - 1) // right-associative
            );
        }

        private Node parseFunctionApplication(Token t, Node lhs)
        {
            return new FunctionApplicationNode(
                lhs: lhs,
                rhs: this.parseExpression(this.m_bps[t.type])
            );
        }

        private Node parseStringConcatenation(Token t, Node lhs)
        {
            return new StringConcatenationNode(
                lhs: lhs,
                rhs: this.parseExpression(this.m_bps[t.type])
            );
        }

        private Node parseSort(Token t, Node lhs)
        {
            //todo: implement
            throw new NotImplementedException();
        }

        private Node parseDot(Token t, Node lhs)
        {
            return new DotNode_(
                lhs: lhs,
                rhs: this.parseExpression(this.m_bps[t.type])
            );
        }

        private Node parseNumericOperator(Token t, Node lhs)
        {
            NumericOperatorNode.NumericOperator op = t.type switch {
                TokenType.typePlus => NumericOperatorNode.NumericOperator.NumericAdd,
                TokenType.typeMinus => NumericOperatorNode.NumericOperator.NumericSubtract,
                TokenType.typeMult => NumericOperatorNode.NumericOperator.NumericMultiply,
                TokenType.typeDiv => NumericOperatorNode.NumericOperator.NumericDivide,
                TokenType.typeMod => NumericOperatorNode.NumericOperator.NumericModulo,
                _ => throw new Exception("Unexpected token " + t.type)
            };

            Node rhs = this.parseExpression(this.m_bps[t.type]);
            return new NumericOperatorNode(op, lhs, rhs);
        }

        private Node parseComparisonOperator(Token t, Node lhs)
        {
            ComparisonOperatorNode.ComparisonOperator op = t.type switch {
                TokenType.typeEqual => ComparisonOperatorNode.ComparisonOperator.ComparisonEqual,
                TokenType.typeNotEqual => ComparisonOperatorNode.ComparisonOperator.ComparisonNotEqual,
                TokenType.typeLess => ComparisonOperatorNode.ComparisonOperator.ComparisonLess,
                TokenType.typeLessEqual => ComparisonOperatorNode.ComparisonOperator.ComparisonLessEqual,
                TokenType.typeGreater => ComparisonOperatorNode.ComparisonOperator.ComparisonGreater,
                TokenType.typeGreaterEqual => ComparisonOperatorNode.ComparisonOperator.ComparisonGreaterEqual,
                TokenType.typeIn => ComparisonOperatorNode.ComparisonOperator.ComparisonIn,
                _ => throw new Exception("Unexpected token " + t.type)
            };

            Node rhs = this.parseExpression(this.m_bps[t.type]);
            return new ComparisonOperatorNode(op, lhs, rhs);
        }

        private Node parseBooleanOperator(Token t, Node lhs)
        {
            BooleanOperatorNode.BooleanOperator op = t.type switch {
                TokenType.typeAnd => BooleanOperatorNode.BooleanOperator.BooleanAnd,
                TokenType.typeOr => BooleanOperatorNode.BooleanOperator.BooleanOr,
                _ => throw new Exception("Unexpected token " + t.type)
            };

            Node rhs = this.parseExpression(this.m_bps[t.type]);
            return new BooleanOperatorNode(op, lhs, rhs);
        }

    }
}

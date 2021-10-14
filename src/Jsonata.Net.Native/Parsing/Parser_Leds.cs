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
            //todo: implement
            throw new NotImplementedException();
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
            //todo: implement
            throw new NotImplementedException();
        }

        private Node parseAssignment(Token t, Node lhs)
        {
            //todo: implement
            throw new NotImplementedException();
        }

        private Node parseFunctionApplication(Token t, Node lhs)
        {
            //todo: implement
            throw new NotImplementedException();
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

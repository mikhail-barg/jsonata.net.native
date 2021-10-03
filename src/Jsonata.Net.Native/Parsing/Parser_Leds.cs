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
            //todo: implement
            throw new NotImplementedException();
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
            //todo: implement
            throw new NotImplementedException();
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
            //todo: implement
            throw new NotImplementedException();
        }

        private Node parseComparisonOperator(Token t, Node lhs)
        {
            //todo: implement
            throw new NotImplementedException();
        }

        private Node parseBooleanOperator(Token t, Node lhs)
        {
            //todo: implement
            throw new NotImplementedException();
        }

    }
}

using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;

namespace Jsonata.Net.Native.Parsing
{
    internal abstract class BaseException : Exception
    {
        protected BaseException()
        {
        }

        protected BaseException(string message) : base(message)
        {
        }

        protected BaseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BaseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    internal sealed class ErrUnterminatedName: BaseException
    {
        public ErrUnterminatedName(string hint)
            :base($"unterminated name (no closing '{hint}')")
        {

        }
    }

    internal sealed class ErrUnterminatedString : BaseException
    {
        public ErrUnterminatedString(string hint)
            : base($"unterminated string literal (no closing '{hint}')")
        {

        }
    }

    internal sealed class ErrSyntaxError : BaseException
    {
        public ErrSyntaxError(Token token)
            : base($"syntax error: '{token}")
        {

        }
    }

    internal sealed class ErrInvalidNumber : BaseException
    {
        public ErrInvalidNumber(Token token)
            : base($"invalid number literal '{token}")
        {

        }
    }

    internal sealed class ErrInvalidBool : BaseException
    {
        public ErrInvalidBool(Token token)
            : base($"invalid bool literal '{token}")
        {

        }
    }

    internal sealed class ErrInfix : BaseException
    {
        public ErrInfix(Token token)
            : base($"the symbol '{token}' cannot be used as an infix operator")
        {

        }
    }

    internal sealed class ErrPrefix : BaseException
    {
        public ErrPrefix(Token token)
            : base($"the symbol '{token}' cannot be used as a prefix operator")
        {

        }
    }

    internal sealed class ErrUnexpectedEOF : BaseException
    {
        public ErrUnexpectedEOF(Token token)
            : base($"unexpected end of expression")
        {

        }
    }

    internal sealed class ErrUnexpectedToken : BaseException
    {
        public ErrUnexpectedToken(TokenType expected, Token actual)
            : base($"expected token '{expected}', got '{actual}'")
        {

        }
    }

    internal sealed class ErrMissingToken : BaseException
    {
        public ErrMissingToken(TokenType expected)
            : base($"expected token '{expected}' before end of expression")
        {

        }
    }

    internal sealed class ErrPathLiteral : BaseException
    {
        public ErrPathLiteral(string hint)
            : base($"invalid path step {hint}: paths cannot contain nulls, strings, numbers or booleans")
        {

        }
    }

    internal sealed class ErrGroupPredicate : BaseException
    {
        public ErrGroupPredicate()
            : base("a predicate cannot follow a grouping expression in a path step")
        {

        }
    }

    internal sealed class ErrBadNumericArguments : BaseException
    {
        public ErrBadNumericArguments(JToken lhs, JToken rhs, NumericOperatorNode node)
            : base($"Failed to evaluate numeric operation {lhs} {NumericOperatorNode.OperatorToString(node.op)} {rhs}")
        {

        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native
{
    /**
     * Error codes
     *
     * Sxxxx    - Static errors (compile time)
     * Txxxx    - Type errors
     * Dxxxx    - Dynamic errors (evaluate time)
     *  01xx    - tokenizer
     *  02xx    - parser
     *  03xx    - regex parser
     *  04xx    - function signature parser/evaluator
     *  10xx    - evaluator
     *  20xx    - operators
     *  3xxx    - functions (blocks of 10 for each function)
     */

    public class JsonataException: Exception
    {
        public string Code { get; }
        public string RawMessage { get; }

        public JsonataException(string code, string message)
            : base($"{code}: {message}")
        {
            this.Code = code;
            this.RawMessage = message;
        }

        protected JsonataException(string code, string message, bool noCodeInMessage)
            : base(message)
        {
            this.Code = code;
            this.RawMessage = message;
        }
    }

    public sealed class JsonataAssertFailedException: JsonataException
    {
        public JsonataAssertFailedException(string message)
            : base("D3141", message, noCodeInMessage: true)
        {
        }
    }
}

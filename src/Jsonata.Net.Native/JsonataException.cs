using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native
{
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

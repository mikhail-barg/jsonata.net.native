using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public sealed class RegexNode: Node
    {
        public System.Text.RegularExpressions.Regex regex { get; }
        public string pattern { get; }

        public RegexNode(System.Text.RegularExpressions.Regex regex, string pattern)
        {
            this.regex = regex;
            this.pattern = pattern;
        }

        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return "/" + this.pattern + "/";
        }
    }
}

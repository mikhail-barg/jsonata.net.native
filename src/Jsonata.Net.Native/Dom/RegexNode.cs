using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public sealed class RegexNode: Node
    {
        public Regex regex { get; }

        public RegexNode(Regex regex)
        {
            this.regex = regex;
        }

        //shorthand constructor for manual DOM construction
        public RegexNode(string regexStr)
            : this(new Regex(regexStr, RegexOptions.Compiled))
        {
        }

        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append('/');
            builder.Append(regex.ToString());
            builder.Append('/');
            if (this.regex.Options.HasFlag(System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                builder.Append('m');
            }
            if (this.regex.Options.HasFlag(System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                builder.Append('i');
            }
            return builder.ToString();
        }

        protected override bool EqualsSpecific(Node other)
        {
            RegexNode otherNode = (RegexNode)other;

            return this.regex.Options == otherNode.regex.Options
                && this.regex.ToString() == otherNode.regex.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Parsing;

namespace Jsonata.Net.Native.Dom
{
    // A PartialNode represents a partially applied function.
    public sealed class PartialNode : Node
    {
        public Node func { get; }
        public IReadOnlyList<Node> args { get; }

        public PartialNode(Node func, IReadOnlyList<Node> args)
        {
            this.func = func;
            this.args = args;
        }

        internal override Node optimize()
        {
            Node func = this.func.optimize();
            List<Node> args = this.args.Select(a => a.optimize()).ToList();
            return new PartialNode(func, args);
        }

        public override string ToString()
        {
            return $"{this.func}({this.args.JoinNodes(", ")})";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Parsing;

namespace Jsonata.Net.Native.Dom
{
    // A FunctionCallNode represents a call to a function.
    public sealed class FunctionCallNode: Node
    {
        public Node func { get; }
        public IReadOnlyList<Node> args { get; }

        public FunctionCallNode(Node func, IReadOnlyList<Node> args)
        {
            this.func = func;
            this.args = args;
        }

        //shorthand constructor for manual DOM construction
        public FunctionCallNode(string functionName, IReadOnlyList<Node> args)
            : this(new VariableNode(functionName), args)
        {
        }

        internal override Node optimize()
        {
            Node func = this.func.optimize();
            List<Node> args = this.args.Select(a => a.optimize()).ToList();
            return new FunctionCallNode(func, args);
        }

        public override string ToString()
        {
            return $"{this.func}({this.args.JoinNodes(", ")})";
        }

        protected override bool EqualsSpecific(Node other)
        {
            FunctionCallNode otherNode = (FunctionCallNode)other;

            return this.func.Equals(otherNode.func)
                && Helpers.NodeListsEqual(this.args, otherNode.args);
        }
    }
}

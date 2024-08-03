using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Parsing;

namespace Jsonata.Net.Native.Dom
{
    // A PathNode represents a JSON object path. It consists of one
    // or more 'steps' or Nodes (most commonly NameNode objects).
    public sealed class PathNode : Node
    {
        private readonly List<Node> m_steps;
        public IReadOnlyList<Node> steps => this.m_steps;
        public bool keepArrays { get; }

        public PathNode(List<Node> steps, bool keepArrays)
        {
            this.m_steps = steps;
            this.keepArrays = keepArrays;
        }

        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            string result = Helpers.JoinNodes(this.steps, ".");
            if (this.keepArrays)
            {
                result += "[]";
            }
            return result;
        }

        internal void ReplaceLastStep(PredicateNode replacement)
        {
            this.m_steps.RemoveAt(this.m_steps.Count - 1);
            this.m_steps.Add(replacement);
        }

        internal PathNode CloneWithKeepArrays()
        {
            return new PathNode(this.m_steps, keepArrays: true);
        }
    }
}

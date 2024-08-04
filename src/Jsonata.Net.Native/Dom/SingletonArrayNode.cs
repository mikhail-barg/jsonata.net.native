using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    // A singletonArrayNode is an interim data structure used when
    // processing path expressions. It is deliberately unexported
    // and gets converted into a PathNode during optimization.
    internal sealed class SingletonArrayNode_ : Node
    {
        private readonly Node m_lhs;

        internal SingletonArrayNode_(Node lhs)
        {
            this.m_lhs = lhs;
        }

        internal override Node optimize()
        {
            Node lhs = this.m_lhs.optimize();
            switch (lhs)
            {
            case PathNode pathNode:
                if (pathNode.keepArrays)
                {
                    return pathNode;
                }
                return pathNode.CloneWithKeepArrays();
            default:
                return new PathNode(new List<Node>() { lhs }, keepArrays: true);
            }
        }

        public override string ToString()
        {
            return $"{this.m_lhs}[]";
        }

        protected override bool EqualsSpecific(Node other)
        {
            throw new NotImplementedException();
        }
    }
}

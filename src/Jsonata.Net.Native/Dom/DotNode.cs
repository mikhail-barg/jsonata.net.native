using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    // A dotNode is an interim structure used to process JSONata path
    // expressions. It is deliberately unexported and creates a PathNode
    // during its optimize phase.
    internal sealed class DotNode_ : Node
    {
        private readonly Node m_lhs;
        private readonly Node m_rhs;

        internal DotNode_(Node lhs, Node rhs)
        {
            this.m_lhs = lhs;
            this.m_rhs = rhs;
        }

        internal override Node optimize()
        {
            List<Node> steps = new List<Node>();
            bool keepArrays = false;

            //lhs
            {
                Node lhs = this.m_lhs.optimize();
                switch (lhs)
                {
                case NumberDoubleNode:
                case NumberIntNode:
                case BooleanNode:
                case NullNode:
                    throw new JsonataException("S0213", $"The literal value {lhs} cannot be used as a step within a path expression");
                case StringNode stringNode:
                    //convert string to NameNode https://github.com/IBM/JSONata4Java/issues/25
                    steps.Add(new FieldNameNode(stringNode.value, escaped: true));
                    break;
                case PathNode pathNode:
                    steps.AddRange(pathNode.steps);
                    keepArrays |= pathNode.keepArrays;
                    break;
                default:
                    steps.Add(lhs);
                    break;
                }
            }

            //rhs
            {
                Node rhs = this.m_rhs.optimize();
                switch (rhs)
                {
                case NumberDoubleNode:
                case NumberIntNode:
                case BooleanNode:
                case NullNode:
                    throw new JsonataException("S0213", $"The literal value {rhs} cannot be used as a step within a path expression");
                case StringNode stringNode:
                    //convert string to NameNode https://github.com/IBM/JSONata4Java/issues/25
                    steps.Add(new FieldNameNode(stringNode.value, escaped: true));
                    break;
                case PathNode pathNode:
                    steps.AddRange(pathNode.steps);
                    keepArrays |= pathNode.keepArrays;
                    break;
                default:
                    steps.Add(rhs);
                    break;
                }
            }

            return new PathNode(steps, keepArrays);
        }

        public override string ToString()
        {
            return $"{this.m_lhs}.{this.m_rhs}";
        }
    }
}

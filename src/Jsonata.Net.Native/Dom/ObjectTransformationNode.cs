using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public sealed class ObjectTransformationNode : Node
    {
        public Node pattern { get; }
        public Node updates { get; }
        public Node? deletes { get; }

        public ObjectTransformationNode(Node pattern, Node updates, Node? deletes) 
        { 
            this.pattern = pattern;
            this.updates = updates;
            this.deletes = deletes;
        }

        internal override Node optimize()
        {
            Node pattern = this.pattern.optimize();
            Node updates = this.updates.optimize();
            Node? deletes = this.deletes?.optimize();
            if (pattern != this.pattern
                || updates != this.updates
                || deletes != this.deletes)
            {
                return new ObjectTransformationNode(pattern, updates, deletes);
            }
            else
            {
                return this;
            }
        }

        public override string ToString()
        {
            if (this.deletes != null)
            {
                return $"|{this.pattern}|{this.updates}, {this.deletes}|";
            }
            else
            {
                return $"|{this.pattern}|{this.updates}|";
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public abstract class Node: IEquatable<Node>
    {
        internal abstract Node optimize();

        //used for DOM comparison only
        public bool Equals(Node? other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.GetType() != this.GetType())
            {
                return false;
            }

            return this.EqualsSpecific(other);
        }

        protected abstract bool EqualsSpecific(Node other);
    }
}

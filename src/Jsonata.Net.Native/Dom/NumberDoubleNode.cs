﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Dom
{
    public sealed class NumberDoubleNode : NumberNode
    {
        public double value { get; }

        public NumberDoubleNode(double value)
        {
            this.value = value;
        }

        internal override Node optimize()
        {
            return this;
        }

        public override string ToString()
        {
            return this.value.ToString();
        }

        public override int GetIntValue()
        {
            return (int)this.value;
        }

        protected override bool EqualsSpecific(Node other)
        {
            NumberDoubleNode otherNode = (NumberDoubleNode)other;

            return Math.Abs(this.value - otherNode.value) <= Double.Epsilon * 2;    //TODO: will fail on NaNs
        }
    }
}

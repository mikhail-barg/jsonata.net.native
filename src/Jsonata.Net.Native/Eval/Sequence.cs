﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;

namespace Jsonata.Net.Native.Eval
{
    internal sealed class Sequence: JArray
    {
        public bool keepSingletons;
        public bool outerWrapper;

        public Sequence()
        {
        }

        public JToken Simplify()
        {
            if (this.ChildrenTokens.Count == 0)
            {
                return EvalProcessor.UNDEFINED;
            }
            else if (this.ChildrenTokens.Count == 1 && !this.keepSingletons)
            {
                return this.ChildrenTokens[0];
            }
            else
            {
                return this;
            }
        }

        protected override JArray DeepCloneArrayNoChildren()
        {
            return new Sequence() {
                keepSingletons = this.keepSingletons,
                outerWrapper = this.outerWrapper
            };
        }
    }
}

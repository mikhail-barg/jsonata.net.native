using System;
using System.Collections.Generic;
using System.Text;
using Jsonata.Net.Native.Json;

namespace Jsonata.Net.Native.Eval
{
    internal sealed class JsonataArray : JArray
    {
        public bool sequence { get; set; }
        public bool cons { get; set; }
        public bool keepSingleton { get; set; }
        public bool outerWrapper { get; set; }
        public bool tupleStream { get; set; }

        internal JsonataArray() { }

        internal JsonataArray(IReadOnlyList<JToken> childTokens)
        {
            this.AddAll(childTokens);
        }

        public static JsonataArray CreateSequence()
        {
            return new JsonataArray() { sequence = true };
        }

        public static JsonataArray CreateSequence(JToken child)
        {
            JsonataArray result = new JsonataArray() { sequence = true };
            result.Add(child);
            return result;
        }

        protected internal override JArray CloneArrayNoChildren()
        {
            JsonataArray result = new JsonataArray();
            result.sequence = this.sequence;
            result.cons = this.cons;
            result.keepSingleton = this.keepSingleton;
            result.outerWrapper = this.outerWrapper;
            result.tupleStream = this.tupleStream;
            return result;
        }

        protected override bool EqualsArrayNoChildren(JArray other)
        {
            //return base.EqualsArrayNoChildren(other);
            if (other is not JsonataArray otherArray)
            {
                return false;
            }
            return this.sequence == otherArray.sequence
                && this.cons == otherArray.cons
                && this.keepSingleton == otherArray.keepSingleton
                && this.outerWrapper == otherArray.outerWrapper
                && this.tupleStream == otherArray.tupleStream;
        }
    }
}

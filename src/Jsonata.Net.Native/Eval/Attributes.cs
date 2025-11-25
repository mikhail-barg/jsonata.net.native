using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.New;

namespace Jsonata.Net.Native.Eval
{
    //provides support for cases when
    // "undefined inputs always return undefined"
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class PropagateUndefinedAttribute: Attribute
    {
    }

    //provides support for cases when
    // "undefined inputs always return undefined"
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class OptionalArgumentAttribute : Attribute
    {
        public readonly object? DefaultValue;

        public OptionalArgumentAttribute(object? defaultValue)
        {
            this.DefaultValue = defaultValue;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class VariableNumberArgumentAsArrayAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class FunctionSignatureAttribute : Attribute
    {
        internal readonly string Signature;

        public FunctionSignatureAttribute(string signature)
        {
            this.Signature = signature;
        }
    }
}

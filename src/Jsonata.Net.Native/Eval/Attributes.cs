using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    //provides support for builtin functions that require EvaluationEnvironment
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class EvalEnvironmentArgumentAttribute : Attribute
    {
    }
}

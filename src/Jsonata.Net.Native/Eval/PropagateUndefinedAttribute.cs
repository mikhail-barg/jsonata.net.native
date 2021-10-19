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
}

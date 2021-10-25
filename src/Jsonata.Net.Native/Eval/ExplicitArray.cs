using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    /** an analogue of 
     Object.defineProperty(result, 'cons', {
                        enumerable: false,
                        configurable: false,
                        value: true
                    })
    */
    internal sealed class ExplicitArray : JArray
    {
        public ExplicitArray()
        {
        }
    }
}

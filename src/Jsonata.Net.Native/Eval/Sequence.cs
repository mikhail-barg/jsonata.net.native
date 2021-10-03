using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    internal sealed class Sequence
    {
        public readonly List<object> values;
        public bool keepSingletons;

        public Sequence(List<object> values)
        {
            this.values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public Sequence(object singleValue)
        {
            this.values = new List<object>() { singleValue };
        }

        public object GetValue()
        {
            if (this.values.Count == 0)
            {
                return Undefined.Instance;
            }
            else if (this.values.Count == 1 && !this.keepSingletons)
            {
                return this.values[0];
            }
            else
            {
                return this;
            }
        }
    }
}

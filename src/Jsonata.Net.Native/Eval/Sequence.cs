using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    [Newtonsoft.Json.JsonConverter(typeof(SequenceConverter))]
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

    internal sealed class SequenceConverter : Newtonsoft.Json.JsonConverter<Sequence>
    {
        public override void WriteJson(JsonWriter writer, Sequence? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value!.values);
        }

        public override Sequence ReadJson(JsonReader reader, Type objectType, Sequence? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
            /*
            string s = (string)reader.Value;

            return new Version(s);
            */
        }
    }

}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    internal sealed class Sequence: JArray
    {
        public bool keepSingletons;

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
    }

    /*
    internal sealed class SequenceConverter : Newtonsoft.Json.JsonConverter<Sequence>
    {
        public override void WriteJson(JsonWriter writer, Sequence? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value!.values);
        }

        public override Sequence ReadJson(JsonReader reader, Type objectType, Sequence? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
            //string s = (string)reader.Value;
            //return new Version(s);
        }
    }
    */

}

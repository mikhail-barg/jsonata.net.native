using System;
using Jsonata.Net.Native.Json;
using NUnit.Framework;

namespace Jsonata.Net.Native.Tests
{
    public sealed class SerializationTest
    {

        //see https://github.com/mikhail-barg/jsonata.net.native/issues/7
        [Test]
        public void Test_Issue7()
        {
            Check("\"Lorem \\\"ipsum\\\"\""); //the json is '"Lorem \"ipsum\""'

            void Check(string source)
            {
                JToken token = JToken.Parse(source);
                string result = token.ToFlatString();
                Assert.AreEqual(source, result);
            }
        }

        private static void CheckSimpleType(object? value, string expectedStr)
        {
            JToken token = JToken.FromObject(value);
            string result = token.ToFlatString();
            Assert.AreEqual(expectedStr, result);
        }

        [Test]
        public void Test_SimpleTypes_byte()
        {
            CheckSimpleType((byte)255, "255");
        }

        [Test]
        public void Test_SimpleTypes_Long()
        {
            CheckSimpleType(Int64.MaxValue, "9223372036854775807");
        }

        [Test]
        public void Test_SimpleTypes_ULong()
        {
            CheckSimpleType(UInt64.MaxValue, "18446744073709551615");
        }


        [Test]
        public void Test_SerializeNulls()
        {
            Check(@"{""b"":null}", @"{""b"":null}", @"{}");
            Check(@"{""a"":1,""b"":null}", @"{""a"":1,""b"":null}", @"{""a"":1}");
            Check(@"{""b"":null,""a"":1}", @"{""b"":null,""a"":1}", @"{""a"":1}");

            void Check(string source, string expectedStrWithNulls, string expectedStrWithNoNulls)
            {
                JToken token = JToken.Parse(source);
                string resultWithNulls = token.ToFlatString(new SerializationSettings() { SerializeNullProperties = true });
                Assert.AreEqual(expectedStrWithNulls, resultWithNulls);
                string resultWithNoNulls = token.ToFlatString(new SerializationSettings() { SerializeNullProperties = false });
                Assert.AreEqual(expectedStrWithNoNulls, resultWithNoNulls);
            }
        }


        [Test]
        public void Test_SerializeStructure()
        {
            Check(
@"{
  ""a"": 1,
  ""b"": {
    ""c"": {}
  },
  ""d"": [
    true,
    false,
    null,
    {
      ""e"": {}
    }
  ]
}"
            );

            void Check(string source)
            {
                source = source.Replace("\r\n", "\n");
                JToken token = JToken.Parse(source);
                string result = token.ToIndentedString();
                Assert.AreEqual(source, result);
            }
        }
    }
}

using System;
using Jsonata.Net.Native.Json;
using NUnit.Framework;

namespace Jsonata.Net.Native.Tests
{
    public sealed class SerializationTest
    {
        private static void Check(string source)
        {
            JToken token = JToken.Parse(source);
            string result = token.ToFlatString();
            Assert.AreEqual(source, result);
        }

        //see https://github.com/mikhail-barg/jsonata.net.native/issues/7
        [Test]
        public void Test_Issue7()
        {
            Check("\"Lorem \\\"ipsum\\\"\""); //the json is '"Lorem \"ipsum\""'
        }

        private static void Check(object? value, string expectedStr)
        {
            JToken token = JToken.FromObject(value);
            string result = token.ToFlatString();
            Assert.AreEqual(expectedStr, result);
        }

        [Test]
        public void Test_SimpleTypes_byte()
        {
            Check((byte)255, "255");
        }

        [Test]
        public void Test_SimpleTypes_Long()
        {
            Check(Int64.MaxValue, "9223372036854775807");
        }

        [Test]
        public void Test_SimpleTypes_ULong()
        {
            Check(UInt64.MaxValue, "18446744073709551615");
        }

    }
}

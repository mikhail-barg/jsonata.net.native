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
    }
}

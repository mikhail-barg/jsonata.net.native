using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jsonata.Net.Native.Json;

namespace Jsonata.Net.Native.Tests
{
    [TestClass]
    public class SerializationTest
    {
        private static void Check(string source)
        {
            JToken token = JToken.Parse(source);
            string result = token.ToFlatString();
            Assert.AreEqual(source, result);
        }

        //see https://github.com/mikhail-barg/jsonata.net.native/issues/7
        [TestMethod]
        public void Test_Issue7()
        {
            Check("\"Lorem \\\"ipsum\\\"\""); //the json is '"Lorem \"ipsum\""'
        }
    }
}

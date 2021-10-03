using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jsonata.Net.Native.Tests
{
    [TestClass]
    public class UnitTest1
    {
        private static void Check(string query, string data, string expectedResult)
        {
            JsonataQuery jsonata = new JsonataQuery(query);
            string resultStr = jsonata.Eval(data);
            JToken resultJson = JToken.Parse(resultStr);
            JToken expectedResultJson = JToken.Parse(expectedResult);
            Assert.IsTrue(JToken.DeepEquals(expectedResultJson, resultJson), $"expected {expectedResult}, got {resultStr}");
        }

        [TestMethod] public void TestSimple_1()
        {
            Check("a", "{'a': 'b'}", "'b'");
        }

        [TestMethod]
        public void TestSimple_2()
        {
            Check("a", "[{'a': 'b'}]", "'b'");
        }

        [TestMethod]
        public void TestSimple_3()
        {
            Check("a", "[{'a': 'b'}, {'a': 'd'}]", "['b', 'd']");
        }

        [TestMethod]
        public void TestSimple_4()
        {
            Check("a.b", "{'a': {'b': 'c'}}", "'c'");
        }

        [TestMethod]
        public void TestSimple_5()
        {
            Check("a.b", "[{'a': {'b': 'c'}}]", "'c'");
        }

        [TestMethod]
        public void TestSimple_6() //TODO: currently fails (
        {
            Check("*.a", "[{'a': 'b'}, {'a': 'd'}]", "['b', 'd']");
        }

        [TestMethod]
        public void TestSimple_7()
        {
            Check("**.a", "[{'a': 'b'}, {'a': 'd'}, {'e': {'a': 'f'}}]", "['b', 'd', 'f']");
        }
    }
}

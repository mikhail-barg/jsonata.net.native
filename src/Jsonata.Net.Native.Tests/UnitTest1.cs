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
            Assert.IsTrue(JToken.DeepEquals(expectedResultJson, resultJson), $"expected {expectedResult}, got {resultJson.ToString(Formatting.None)}");
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

        [TestMethod]
        public void TestFlatten_1()
        {
            Check(
                "nest2.[nest3]", 
                @"{
                    'nest2': [
                        {
                            'nest3': [ 1 ]
                        },
                        {
                            'nest3': [ 2 ]
                        }
                    ]
                }",
                "[[1],[2]]"
            );
        }

        [TestMethod]
        public void TestFlatten_2()
        {
            Check(
                "nest2.[nest3]",
                @"{
                    'nest2': [
                        {
                            'nest3': [ 1 ]
                        }
                    ]
                }",
                "[1]"
            );
        }

        [TestMethod]
        public void TestFlatten_3()
        {
            Check(
                "nest3",
                @"{
                    'nest3': [ 1 ]
                }",
                "[1]"
            );
        }

        [TestMethod]
        public void TestFlatten_4()
        {
            Check(
                "[[1]]",
                @"{}",
                "[[1]]"
            );
        }

        [TestMethod]
        public void TestFlatten_5()
        {
            Check(
                "[[1],[2]]",
                @"{}",
                "[[1],[2]]"
            );
        }
    }
}

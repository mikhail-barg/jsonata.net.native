using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Jsonata.Net.Native.Tests
{
    public class UnitTest1
    {
        private static void Check(string query, string data, string expectedResult)
        {
            JsonataQuery jsonata = new JsonataQuery(query);
            string resultStr = jsonata.Eval(data);
            JToken resultJson = JToken.Parse(resultStr);
            JToken expectedResultJson = JToken.Parse(expectedResult);
            Console.WriteLine("Expected: " + expectedResultJson.ToString(Formatting.None));
            Console.WriteLine("Got: " + resultJson.ToString(Formatting.None));
            Assert.IsTrue(JToken.DeepEquals(expectedResultJson, resultJson), $"expected {expectedResult}, got {resultJson.ToString(Formatting.None)}");
        }

        [Test] 
        public void TestSimple_1()
        {
            Check("a", "{'a': 'b'}", "'b'");
        }

        [Test]
        public void TestSimple_2()
        {
            Check("a", "[{'a': 'b'}]", "'b'");
        }

        [Test]
        public void TestSimple_3()
        {
            Check("a", "[{'a': 'b'}, {'a': 'd'}]", "['b', 'd']");
        }

        [Test]
        public void TestSimple_4()
        {
            Check("a.b", "{'a': {'b': 'c'}}", "'c'");
        }

        [Test]
        public void TestSimple_5()
        {
            Check("a.b", "[{'a': {'b': 'c'}}]", "'c'");
        }

        [Test]
        public void TestSimple_6() //TODO: currently fails (
        {
            Check("*.a", "[{'a': 'b'}, {'a': 'd'}]", "['b', 'd']");
        }

        [Test]
        public void TestSimple_7()
        {
            Check("**.a", "[{'a': 'b'}, {'a': 'd'}, {'e': {'a': 'f'}}]", "['b', 'd', 'f']");
        }

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
        public void TestFlatten_4()
        {
            Check(
                "[[1]]",
                @"{}",
                "[[1]]"
            );
        }

        [Test]
        public void TestFlatten_5()
        {
            Check(
                "[[1],[2]]",
                @"{}",
                "[[1],[2]]"
            );
        }

        [Test]
        public void TestFunctionContextBroadcast_1()
        {
            Check(
                "['1', '2', '3', '4', '5'].$number($)",
                @"{}",
                "[1,2,3,4,5]"
            );
        }

        [Test]
        public void TestFunctionContextBroadcast_2()
        {
            Check(
                "['1', '2', '3', '4', '5'].$number()",
                @"{}",
                "[1,2,3,4,5]"
            );
        }

        [Test]
        public void TestFunctionContextBroadcast_pad1()
        {
            Check(
                "$pad('str', 5)",
                @"'ctx'",
                "'str  '"
            );
        }

        [Test]
        public void TestFunctionContextBroadcast_pad2()
        {
            Check(
                "$pad('str', 5, '-')",
                @"'ctx'",
                "'str--'"
            );
        }

        [Test]
        public void TestFunctionContextBroadcast_pad3()
        {
            Check(
                "$pad(5, '-')",
                @"'ctx'",
                "'ctx--'"
            );
        }

        [Test]
        public void TestFunctionContextBroadcast_pad4()
        {
            Check(
                "$pad(5)",
                @"'ctx'",
                "'ctx  '"
            );
        }

        [Test]
        public void TestTransform_1()
        {
            Check(
                "$ ~> |$|{'x': 'y'}|",
                @"{'a': 'b'}",
                "{'a': 'b', 'x': 'y'}"
            );
        }

        [Test]
        public void TestTransform_2()
        {
            Check(
                "$ ~> |a|{'x': 'y'}|",
                @"{'a': {'b': 'c'}}",
                "{'a': {'b': 'c', 'x': 'y'}}"
            );
        }

        [Test]
        public void Test_Issue3_1()
        {
            Check(
                @"(
                    $foo:= function($o){
                        $o
                    };
                    $cl:= $foo();
                    $[].{ }
                )",
                "[{'a': 1}, {'a': 2}, {'a': 3}]",
                "[{},{},{}]"
            );
        }

        [Test]
        public void Test_Issue3_2()
        {
            Check(
                @"(
                    $foo:= function($o){
                        $o.z
                    };
                    $cl:= $foo();
                    $[].{ }
                )",
                "[{'a': 1}, {'a': 2}, {'a': 3}]",
                "[{},{},{}]"
            );
        }


        [Test]
        public void Test_Issue9_1()
        {
            Check(
                @"$ {V : 'z'}",
                @"[{ 'Name': 'Foo', 'V': 'SomeName'}]",
                @"{ 'SomeName': 'z' }"
            );
        }

        [Test]
        public void Test_Issue9_2()
        {
            Check(
                @"$ {V : 'z'}",
                @"[]",
                @"{}"
            );
        }


        [Test]
        public void Test_Parent_1()
        {
            Check(
                @"a.%",
                @"{'a': 'b'}",
                @"{'a': 'b'}"
            );
        }

        [Test]
        public void Test_Parent_2()
        {
            Check(
                @"a.%",
                @"[{'a': 'b'}, {'a': 'c'}]",
                @"[{'a': 'b'}, {'a': 'c'}, {'a': 'b'}, {'a': 'c'}]"
            );
        }


        [Test]
        public void Test_Parent_3()
        {
            Check(
                @"Account.Order.%",
                @"{'Account': { 'Name': 'F', 'Order': ''} }",
                @"{ 'Name': 'F', 'Order': ''}"
            );
        }

        [Test]
        public void Test_Parent_4()
        {
            Check(
                @"data.name.%.id",
                @"{ 'data': [ { 'id': 1, 'name': 'a' }, { 'id': 2, 'name': 'b' } ] }",
                @"[ 1, 2 ]"
            );
        }

        [Test]
        public void Test_Parent_5()
        {
            Check(
                @"data.name.{ 
                    'ident': %.id
                }",
                @"{ 'data': [ { 'id': 1, 'name': 'a' }, { 'id': 2, 'name': 'b' } ] }",
                @"[ { 'ident': 1 }, { 'ident': 2 } ]"
            );
        }

        [Test]
        public void Test_Parent_6()
        {
            Check(
                @"data.(name).%",
                @"{ 'data': [ { 'id': 1, 'name': 'a' }, { 'id': 2, 'name': 'b' } ] }",
                @"[ { 'id': 1, 'name': 'a' }, { 'id': 2, 'name': 'b' } ]"
            );
        }

        [Test]
        public void Test_Parent_7()
        {
            Check(
                @"$.(data.name).%",
                @"{ 'data': [ { 'id': 1, 'name': 'a' }, { 'id': 2, 'name': 'b' } ] }",
                @"[ { 'id': 1, 'name': 'a' }, { 'id': 2, 'name': 'b' } ]"
            );
        }
    }
}

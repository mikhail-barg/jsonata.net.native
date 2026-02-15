using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.IO;

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

        private static void CheckFromFile(string query, string dataFileName, string expectedResult)
        {
            JsonataQuery jsonata = new JsonataQuery(query);
            string resultStr = jsonata.Eval(File.ReadAllText(dataFileName));
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
        public void TestSimple_8()
        {
            Check("0.000000000001", "{}", "0.000000000001");
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

        [Test]
        public void Test_Issue14_1()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = DateTime.Now });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken);
            Assert.Pass();
        }


        [Test]
        public void Test_Issue14_2()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = Guid.NewGuid() });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken);
            Assert.Pass();
        }

        [Test]
        public void Test_Issue14_3()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = TimeSpan.FromSeconds(5) });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken);
            Assert.Pass();
        }

        [Test]
        public void Test_Issue14_4()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = new Uri("http://abc.xyz") });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken);
            Assert.Pass();
        }

        [Test]
        public void Test_Issue14_5()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = DateTimeOffset.Now });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken);
            Assert.Pass();
        }

        [Test]
        public void Test_Issue14_6()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = new DateTime(2023, 09, 17, 22, 28, 00) });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken, CultureInfo.InvariantCulture, datetimeFormat: "yyyy~MM~dd HH:mm:ss");
            string value = (string)((Jsonata.Net.Native.Json.JObject)jToken).Properties["key"];
            Assert.AreEqual("2023~09~17 22:28:00", value);
        }

        [Test]
        public void Test_Issue14_7()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = new TimeSpan(10, 12, 13, 14, 156) });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken, CultureInfo.InvariantCulture, timespanFormat: @"ddd\-hh\-mm\-ss\-fff");
            string value = (string)((Jsonata.Net.Native.Json.JObject)jToken).Properties["key"];
            Assert.AreEqual("010-12-13-14-156", value);
        }

        [Test]
        public void Test_Issue14_8()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = Guid.Empty });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken, CultureInfo.InvariantCulture, guidFormat: "N");
            string value = (string)((Jsonata.Net.Native.Json.JObject)jToken).Properties["key"];
            Assert.AreEqual("00000000000000000000000000000000", value);
        }

        [Test]
        public void Test_Issue34()
        {
            Check(
                @"$map($, function($v){ $map($v, function($t) { $t }) })",
                @"[[1,2],[1]]",
                @"[[1,2],[1]]"
            );
        }

        [Test]
        public void Test_Issue29()
        {
            Check(
                @"$ ~> | $ | {""test"": ([] ~> $append(test))}|",
                @"{ 'test': [ { 'test': 'test' } ] }",
                @"{ 'test': [ { 'test': 'test' } ] }"
            );
        }

        [Test]
        public void Test_Issue43_Sorting_WorksOnSmallSets()
        {
            Check(
                @"$sort($, function($l, $r) {$l.Properties[Name='age'].Value > $r.Properties[Name='age'].Value})",
                @"[{'name':'Bernard','Properties':[{'Name':'age','Value':33}]},{'name':'Astrid','Properties':[{'Name':'age','Value':22}]},{'name':'Charley','Properties':[{'Name':'age 2','Value':55}]}]",
                @"[{'name':'Astrid','Properties':[{'Name':'age','Value':22}]},{'name':'Bernard','Properties':[{'Name':'age','Value':33}]},{'name':'Charley','Properties':[{'Name':'age 2','Value':55}]}]"
            );
        }



        [Test]
        public void Test_Issue43_SortingWithManyItems()
        {
            Check(
                @"$sort($, function($l, $r) {$l.Properties[Name='age'].Value > $r.Properties[Name='age'].Value})",
                @"[{'name':'Bernard','Properties':[{'Name':'age','Value':33}]},{'name':'Astrid','Properties':[{'Name':'age','Value':22}]},{'name':'Charley','Properties':[{'Name':'age 2','Value':55}]},{'name':'Bernard','Properties':[{'Name':'age','Value':33}]},{'name':'Astrid','Properties':[{'Name':'age','Value':22}]},{'name':'Charley','Properties':[{'Name':'age 2','Value':55}]},{'name':'Bernard','Properties':[{'Name':'age','Value':33}]},{'name':'Astrid','Properties':[{'Name':'age','Value':22}]},{'name':'Charley','Properties':[{'Name':'age 2','Value':55}]},{'name':'Bernard','Properties':[{'Name':'age','Value':33}]},{'name':'Astrid','Properties':[{'Name':'age','Value':22}]},{'name':'Charley','Properties':[{'Name':'age 2','Value':55}]},{'name':'Bernard','Properties':[{'Name':'age','Value':33}]},{'name':'Astrid','Properties':[{'Name':'age','Value':22}]},{'name':'Charley','Properties':[{'Name':'age 2','Value':55}]},{'name':'Bernard','Properties':[{'Name':'age','Value':33}]},{'name':'Astrid','Properties':[{'Name':'age','Value':22}]},{'name':'Charley','Properties':[{'Name':'age 2','Value':55}]}]",
                @"[{'name':'Bernard','Properties':[{'Name':'age','Value':33}]},{'name':'Bernard','Properties':[{'Name':'age','Value':33}]},{'name':'Charley','Properties':[{'Name':'age 2','Value':55}]},{'name':'Astrid','Properties':[{'Name':'age','Value':22}]},{'name':'Bernard','Properties':[{'Name':'age','Value':33}]},{'name':'Charley','Properties':[{'Name':'age 2','Value':55}]},{'name':'Astrid','Properties':[{'Name':'age','Value':22}]},{'name':'Bernard','Properties':[{'Name':'age','Value':33}]},{'name':'Charley','Properties':[{'Name':'age 2','Value':55}]},{'name':'Astrid','Properties':[{'Name':'age','Value':22}]},{'name':'Bernard','Properties':[{'Name':'age','Value':33}]},{'name':'Charley','Properties':[{'Name':'age 2','Value':55}]},{'name':'Astrid','Properties':[{'Name':'age','Value':22}]},{'name':'Bernard','Properties':[{'Name':'age','Value':33}]},{'name':'Charley','Properties':[{'Name':'age 2','Value':55}]},{'name':'Astrid','Properties':[{'Name':'age','Value':22}]},{'name':'Astrid','Properties':[{'Name':'age','Value':22}]},{'name':'Charley','Properties':[{'Name':'age 2','Value':55}]}]"
            );
        }

        [Test]
        public void Test_Sort()
        {
            Check(
                @"$^($)",
                @"[34.45,21.67,34.45,107.99]",
                @"[21.67,34.45,34.45,107.99]"
            );
        }

        [Test]
        public void Test_Join1()
        {
            CheckFromFile(
                """
                library.loans@$l#$il.books@$b#$ib[$l.isbn=$b.isbn]#$ib2.customers@$c#$ic[$l.customer=$c.id].{
                  'title': $b.title,
                  'customer': $l.customer,
                  'name': $c.name,
                  'loan-index': $il,
                  'book-index': $ib,
                  'customer-index': $ic,
                  'ib2': $ib2
                }
                """,
                "../../../../../jsonata-js/test/test-suite/datasets/library.json",
                """
                [
                  {
                    "title": "Structure and Interpretation of Computer Programs",
                    "customer": "10001",
                    "name": "Joe Doe",
                    "loan-index": 0,
                    "book-index": 0,
                    "customer-index": 0,
                    "ib2": 0
                  },
                  {
                    "title": "Compilers: Principles, Techniques, and Tools",
                    "customer": "10003",
                    "name": "Jason Arthur",
                    "loan-index": 1,
                    "book-index": 3,
                    "customer-index": 2,
                    "ib2": 1
                  },
                  {
                    "title": "Structure and Interpretation of Computer Programs",
                    "customer": "10003",
                    "name": "Jason Arthur",
                    "loan-index": 2,
                    "book-index": 0,
                    "customer-index": 2,
                    "ib2": 2
                  }
                ]
                """
            );
        }

        [Test]
        public void Test_Join2()
        {
            CheckFromFile(
                """
                library.loans@$l#$il.books@$b#$ib[$l.isbn=$b.isbn]#$ib2.{
                  'title': $b.title,
                  'customer': $l.customer,
                  'name': $c.name,
                  'loan-index': $il,
                  'book-index': $ib,
                  'customer-index': $ic,
                  'ib2': $ib2
                }
                """,
                "../../../../../jsonata-js/test/test-suite/datasets/library.json",
                """
                [
                  {
                    "title": "Structure and Interpretation of Computer Programs",
                    "customer": "10001",
                    "loan-index": 0,
                    "book-index": 0,
                    "ib2": 0
                  },
                  {
                    "title": "Compilers: Principles, Techniques, and Tools",
                    "customer": "10003",
                    "loan-index": 1,
                    "book-index": 3,
                    "ib2": 1
                  },
                  {
                    "title": "Structure and Interpretation of Computer Programs",
                    "customer": "10003",
                    "loan-index": 2,
                    "book-index": 0,
                    "ib2": 2
                  }
                ]
                """
            );
        }

        [Test]
        public void Test_Join3()
        {
            CheckFromFile(
                """
                library.loans@$l#$il.books@$b#$ib[$l.isbn=$b.isbn].{
                  'title': $b.title,
                  'customer': $l.customer,
                  'name': $c.name,
                  'loan-index': $il,
                  'book-index': $ib,
                  'customer-index': $ic,
                  'ib2': $ib2
                }
                """,
                "../../../../../jsonata-js/test/test-suite/datasets/library.json",
                """
                [
                  {
                    "title": "Structure and Interpretation of Computer Programs",
                    "customer": "10001",
                    "loan-index": 0,
                    "book-index": 0
                  },
                  {
                    "title": "Compilers: Principles, Techniques, and Tools",
                    "customer": "10003",
                    "loan-index": 1,
                    "book-index": 3
                  },
                  {
                    "title": "Structure and Interpretation of Computer Programs",
                    "customer": "10003",
                    "loan-index": 2,
                    "book-index": 0
                  }
                ]
                """
            );
        }

        [Test]
        public void Test_Split()
        {
            Check(
                @"$split(""Hello"", "" "")",
                @"{}",
                @"['Hello']"
            );
        }

        [Test]
        public void Test_Filter()
        {
            Check(
                @"Product[$.""Product Name"" ~> /hat/i].ProductID",
                @"
                    {
                      'Product': [
                        {
                          'ProductID': 345664,
                          'Product Name': 'Cloak'
                        }
                      ]
                    }

                ",
                @"undefined"
            );
        }

        [Test]
        public void Test_Eval()
        {
            Check(
                @"$eval('Quantity ~> $sum()')",
                @"
                    {
                        'Quantity': 2
                    }
                ",
                "2"
            );
        }

        [Test]
        public void Test_Zip()
        {
            Check(
                @"$zip([1,2,3],[4,5,6])",
                @"{}",
                "[[1,4],[2,5],[3,6]]"
            );
        }

        [Test]
        public void Test_Zip2()
        {
            Check(
                @"$zip([1,2,3],[4,5,6],[7,8])",
                @"{}",
                "[[1,4,7],[2,5,8]]"
            );
        }
    }
}

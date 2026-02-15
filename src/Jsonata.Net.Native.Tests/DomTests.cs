using System;
using System.Collections.Generic;
using Jsonata.Net.Native;
using Jsonata.Net.Native.Impl;
using NUnit.Framework;

namespace Jsonata.Net.Native.Tests
{
    public class DomTests
    {

        private void CheckStructure(JsonataQuery expectedQuery, JsonataQuery constructedQuery)
        {
            Node expectedAst = expectedQuery.GetAst();
            Node constructedAst = constructedQuery.GetAst();
            Assert.IsTrue(expectedAst.Equals(constructedAst), "Structural comparison failed");
        }


        [Test] 
        public void TestSimple_1()
        {
            string expectedQueryStr = "$x := $count($foo) > 0";
            JsonataQuery expectedQuery = new JsonataQuery(expectedQueryStr);
            Console.WriteLine("expected query:");
            Console.WriteLine(expectedQuery.GetAst().PrintAst());
            Node node = new AssignVarConstructionNode(
                "x",
                new BinaryNode(
                    BinaryOperatorType.gt,
                    new FunctionalNode("count", [ new VariableNode("foo") ]),
                    new NumberIntNode(0)
                )
            );
            JsonataQuery query = JsonataQuery.FromAst(node);
            Console.WriteLine("Built query:");
            Console.WriteLine(query.GetAst().PrintAst());
            string result = query.Eval("{}");
            Assert.AreEqual("false", result);
            CheckStructure(expectedQuery, query);
        }


        [Test]
        public void TestSimple_2()
        {
            string expectedQueryStr = @"
                (
                  $factorial := function($x) {
                    $x <= 1 ? 1 : $x * $factorial($x-1)
                  };
                  $factorial(5)
                )             
            ";
            JsonataQuery expectedQuery = new JsonataQuery(expectedQueryStr);
            Console.WriteLine("expected query:");
            Console.WriteLine(expectedQuery.GetAst().PrintAst());

            Node node = new BlockNode(
                [
                    new AssignVarConstructionNode(
                        "factorial",
                        new LambdaNode(
                            arguments: [ new VariableNode("x") ],
                            body: new ConditionNode(
                                condition: new BinaryNode(
                                    BinaryOperatorType.le,
                                    new VariableNode("x"),
                                    new NumberIntNode(1)
                                ),
                                then: new NumberIntNode(1),
                                @else: new BinaryNode(
                                    BinaryOperatorType.mul,
                                    new VariableNode("x"),
                                    new FunctionalNode(
                                        "factorial",
                                        [
                                            new BinaryNode(
                                                BinaryOperatorType.sub,
                                                new VariableNode("x"),
                                                new NumberIntNode(1)
                                            )
                                        ]
                                    )
                                )
                            )
                        )
                    ),
                    new FunctionalNode("factorial", [ new NumberIntNode(5) ] )
                ]
            );
            JsonataQuery query = JsonataQuery.FromAst(node);
            Console.WriteLine("Built query:");
            Console.WriteLine(query.GetAst().PrintAst());
            string result = query.Eval("{}");
            Assert.AreEqual("120", result);
            CheckStructure(expectedQuery, query);
        }
    }
}

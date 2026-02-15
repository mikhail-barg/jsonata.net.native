using System;
using System.Collections.Generic;
using Jsonata.Net.Native;
using Jsonata.Net.Native.New;
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
            Node node = new BindAssignVarConstructionNode(
                new VariableNode("x"),
                new BinaryNode(
                    BinaryOperatorType.gt,
                    new FunctionalNode(
                        false,
                        new VariableNode("count"),
                        new List<Node>() {
                            new VariableNode("foo")
                        }
                    ),
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
                new List<Node> {
                    new BindAssignVarConstructionNode(
                        new VariableNode("factorial"),
                        new LambdaNode(
                            arguments: new List<VariableNode>(){ new VariableNode("x") },
                            signature: null,
                            body: new ConditionNode(
                                new BinaryNode(
                                    BinaryOperatorType.le,
                                    new VariableNode("x"),
                                    new NumberIntNode(1)
                                ),
                                new NumberIntNode(1),
                                new BinaryNode(
                                    BinaryOperatorType.mul,
                                    new VariableNode("x"),
                                    new FunctionalNode(
                                        false,
                                        new VariableNode("factorial"),
                                        new List<Node>() {
                                            new BinaryNode(
                                                BinaryOperatorType.sub,
                                                new VariableNode("x"),
                                                new NumberIntNode(1)
                                            )
                                        }
                                    )
                                )
                            ),
                            thunk: false
                        )
                    ),
                    new FunctionalNode(
                        false,
                        new VariableNode("factorial"),
                        new List<Node>() {
                            new NumberIntNode(5)
                        }
                    )
                }
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

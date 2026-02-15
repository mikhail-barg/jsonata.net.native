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
                -1,
                new VariableNode(-1, "x"),
                new BinaryNode(
                    -1,
                    BinaryOperatorType.gt,
                    new FunctionalNode(
                        SymbolType.function,
                        -1,
                        new VariableNode(-1, "count"),
                        new List<Node>() {
                            new VariableNode(-1, "foo")
                        }
                    ),
                    new NumberIntNode(-1, 0)
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
                -1,
                new List<Node> {
                    new BindAssignVarConstructionNode(
                        -1,
                        new VariableNode(-1, "factorial"),
                        new LambdaNode(
                            position: -1,
                            arguments: new List<VariableNode>(){ new VariableNode(-1, "x") },
                            signature: null,
                            body: new ConditionNode(
                                -1,
                                new BinaryNode(
                                    -1,
                                    BinaryOperatorType.le,
                                    new VariableNode(-1, "x"),
                                    new NumberIntNode(-1, 1)
                                ),
                                new NumberIntNode(-1, 1),
                                new BinaryNode(
                                    -1,
                                    BinaryOperatorType.mul,
                                    new VariableNode(-1, "x"),
                                    new FunctionalNode(
                                        SymbolType.function,
                                        -1,
                                        new VariableNode(-1, "factorial"),
                                        new List<Node>() {
                                            new BinaryNode(
                                                -1,
                                                BinaryOperatorType.sub,
                                                new VariableNode(-1, "x"),
                                                new NumberIntNode(-1, 1)
                                            )
                                        }
                                    )
                                )
                            ),
                            thunk: false
                        )
                    ),
                    new FunctionalNode(
                        SymbolType.function,
                        -1,
                        new VariableNode(-1, "factorial"),
                        new List<Node>() {
                            new NumberIntNode(-1, 5)
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

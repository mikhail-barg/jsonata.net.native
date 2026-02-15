using System;
using System.Collections.Generic;
using Jsonata.Net.Native;
using Jsonata.Net.Native.New;
using NUnit.Framework;

namespace Jsonata.Net.Native.Tests
{
    public class DomTests
    {

        private void CheckStructure(string expectedQueryString, Node value)
        {
            JsonataQuery expectedQuery = new JsonataQuery(expectedQueryString);
            Assert.IsTrue(expectedQuery.GetAst().Equals(value), "Structural comparison failed");
        }


        [Test] 
        public void TestSimple_1()
        {
            //string expectedQuery = "$x := $count($foo) > 0";
            //JsonataQuery query = new JsonataQuery(expectedQuery);
            //Console.WriteLine(query.GetAst().PrintAst());
            //throw new NotImplementedException();
            Node node = new BindAssignVarConstructionNode(
                -1,
                new VariableNode(-1, "x"),
                new BinaryNode(
                    -1,
                    BinaryOperatorType.gt,
                    new FunctionalNode(
                        SymbolType.function,
                        -1,
                        new NameNode(-1, "count"),
                        new List<Node>() {
                            new VariableNode(-1, "foo")
                        }
                    ),
                    new NumberIntNode(-1, 0)
                )
            );
            throw new NotImplementedException();
            /*
            JsonataQuery query = new JsonataQuery.FromAst(node);
            string result = query.Eval("{}");
            Assert.AreEqual("false", result);
            CheckStructure(expectedQuery, node);
            */
        }


        [Test]
        public void TestSimple_2()
        {
            throw new NotImplementedException();
            /*
            string expectedQuery = @"
                (
                  $factorial := function($x) {
                    $x <= 1 ? 1 : $x * $factorial($x-1)
                  };
                  $factorial(5)
                )             
            ";

            Node node = new BlockNode(
                new List<Node> {
                    new AssignmentNode(
                        "factorial",
                        new LambdaNode(
                            new List<string>(){ "x" },
                            new ConditionalNode(
                                new ComparisonOperatorNode(
                                    ComparisonOperatorNode.Operator.LessEqual,
                                    new VariableNode("x"),
                                    new NumberIntNode(1)
                                ),
                                new NumberIntNode(1),
                                new NumericOperatorNode(
                                    NumericOperatorNode.Operator.Multiply,
                                    new VariableNode("x"),
                                    new FunctionCallNode(
                                        "factorial",
                                        new List<Node>() {
                                            new NumericOperatorNode(
                                                NumericOperatorNode.Operator.Subtract,
                                                new VariableNode("x"),
                                                new NumberIntNode(1)
                                            )
                                        }
                                    )
                                )
                            )
                        )
                    ),
                    new FunctionCallNode(
                        "factorial",
                        new List<Node>() {
                            new NumberIntNode(5)
                        }
                    )
                }
            );
            JsonataQuery query = new JsonataQuery(node);
            string result = query.Eval("{}");
            Assert.AreEqual("120", result);
            CheckStructure(expectedQuery, node);
            */
        }
    }
}

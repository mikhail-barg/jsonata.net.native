using System;
using System.Collections.Generic;
using System.Globalization;
using Jsonata.Net.Native.Dom;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            throw new NotImplementedException();
            /*
            string expectedQuery = "$x := $count($foo) > 0";
            Node node = new AssignmentNode(
                "x",
                new ComparisonOperatorNode(
                    ComparisonOperatorNode.Operator.Greater,
                    new FunctionCallNode(
                        "count",
                        new List<Node>() { new VariableNode("foo") }
                    ),
                    new NumberIntNode(0)
                )
            );
            JsonataQuery query = new JsonataQuery(node);
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

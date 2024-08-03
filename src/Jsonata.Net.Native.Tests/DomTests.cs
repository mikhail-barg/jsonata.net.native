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


        [Test] 
        public void TestSimple_1()
        {
            //$x := $count($foo) > 0;
            Node node = new AssignmentNode(
                "x",
                new ComparisonOperatorNode(
                    ComparisonOperatorNode.ComparisonOperator.ComparisonGreater,
                    new FunctionCallNode(
                        new VariableNode("count"),
                        new List<Node>() { new VariableNode("foo") }
                    ),
                    new NumberIntNode(0)
                )
            );
            JsonataQuery query = new JsonataQuery(node);
            string result = query.Eval("{}");
            Assert.AreEqual("false", result);
        }


        [Test]
        public void TestSimple_2()
        {
            /*
                (
                  $factorial := function($x) {
                    $x <= 1 ? 1 : $x * $factorial($x-1)
                  };
                  $factorial(5)
                )             
            */

            Node node = new BlockNode(
                new List<Node> {
                    new AssignmentNode(
                        "factorial",
                        new LambdaNode(
                            false,
                            new List<string>(){ "x" },
                            null,
                            new ConditionalNode(
                                new ComparisonOperatorNode(
                                    ComparisonOperatorNode.ComparisonOperator.ComparisonLessEqual,
                                    new VariableNode("x"),
                                    new NumberIntNode(1)
                                ),
                                new NumberIntNode(1),
                                new NumericOperatorNode(
                                    NumericOperatorNode.NumericOperator.NumericMultiply,
                                    new VariableNode("x"),
                                    new FunctionCallNode(
                                        new VariableNode("factorial"),
                                        new List<Node>() {
                                            new NumericOperatorNode(
                                                NumericOperatorNode.NumericOperator.NumericSubtract,
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
                        new VariableNode("factorial"),
                        new List<Node>() {
                            new NumberIntNode(5)
                        }
                    )
                }
            );
            JsonataQuery query = new JsonataQuery(node);
            string result = query.Eval("{}");
            Assert.AreEqual("120", result);
        }
    }
}

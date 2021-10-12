using Jsonata.Net.Native.Parsing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
	internal static class EvalProcessor
	{
		internal static readonly JValue UNDEFINED = JValue.CreateUndefined();


		internal static JToken EvaluateJson(Node rootNode, JToken data)
		{
			//TODO: prepare environment
			Environment environment = new Environment();
			if (data is JArray)
            {
				JArray dataArr = new Sequence() { keepSingletons = true };
				dataArr.Add(data);
				data = dataArr;
            }
			JToken result = Eval(rootNode, data, environment);
			if (result is Sequence seq)
            {
				//result = seq.GetValue();
				if (seq.Count == 1 && !seq.keepSingletons)
				{
					result = seq.Children().First();
				}
			}
			return result;
		}

		internal static JToken Eval(Node node, JToken input, Environment env)
		{
			switch (node)
			{
			case StringNode stringNode:
				return evalString(stringNode, input, env);
			case NumberDoubleNode numberDoubleNode:
				return evalNumber(numberDoubleNode, input, env);
			case NumberIntNode numberIntNode:
				return evalNumber(numberIntNode, input, env);
			case BooleanNode booleanNode:
				return evalBoolean(booleanNode, input, env);
			case NullNode nullNode:
				return evalNull(nullNode, input, env);
			/*
			case RegexNode:
				return evalRegex(node, input, env);
			case VariableNode:
				return evalVariable(node, input, env);
			*/
			case NameNode nameNode:
				return evalName(nameNode, input, env);
			case PathNode pathNode:
				return evalPath(pathNode, input, env);
			/*
			case NegationNode:
				return evalNegation(node, input, env);
			case RangeNode:
				return evalRange(node, input, env);
			*/
			case ArrayNode arrayNode:
				return evalArray(arrayNode, input, env);
			/*
			case ObjectNode objectNode:
				return evalObject(objectNode, input, env);
			/*
			case BlockNode:
				return evalBlock(node, input, env);
			case ConditionalNode:
				return evalConditional(node, input, env);
			case AssignmentNode:
				return evalAssignment(node, input, env);
			*/
			case WildcardNode wildcardNode:
				return evalWildcard(wildcardNode, input, env);
			case DescendentNode descendentNode:
				return evalDescendent(descendentNode, input, env);
			/*	
			case GroupNode:
				return evalGroup(node, input, env);
			case PredicateNode:
				return evalPredicate(node, input, env);
			case SortNode:
				return evalSort(node, input, env);
			case LambdaNode:
				return evalLambda(node, input, env);
			case TypedLambdaNode:
				return evalTypedLambda(node, input, env);
			case ObjectTransformationNode:
				return evalObjectTransformation(node, input, env);
			case PartialNode:
				return evalPartial(node, input, env);
			case FunctionCallNode:
				return evalFunctionCall(node, input, env);
			case FunctionApplicationNode:
				return evalFunctionApplication(node, input, env);
			*/
			case NumericOperatorNode numericOperatorNode:
				return evalNumericOperator(numericOperatorNode, input, env);
			/*
			case ComparisonOperatorNode:
				return evalComparisonOperator(node, input, env);
			case BooleanOperatorNode:
				return evalBooleanOperator(node, input, env);
			case StringConcatenationNode:
				return evalStringConcatenation(node, input, env);
			*/
			default:
				throw new Exception($"eval: unexpected node type {node.GetType().Name}: {node}");
			}
		}

        private static JToken evalNumericOperator(NumericOperatorNode numericOperatorNode, JToken input, Environment env)
        {
			JToken lhs = Eval(numericOperatorNode.lhs, input, env);
			JToken rhs = Eval(numericOperatorNode.rhs, input, env);
			if (lhs.Type == JTokenType.Undefined || rhs.Type == JTokenType.Undefined)
            {
				return EvalProcessor.UNDEFINED;
            }
			else if (lhs.Type == JTokenType.Integer && rhs.Type == JTokenType.Integer)
            {
				return evalIntOperator((long)lhs, (long)rhs, numericOperatorNode.op);
            }
			else if (lhs.Type == JTokenType.Float && rhs.Type == JTokenType.Float)
            {
				return evalDoubleOperator((double)lhs, (double)rhs, numericOperatorNode.op);
			}
			else if (lhs.Type == JTokenType.Float && rhs.Type == JTokenType.Integer)
			{
				return evalDoubleOperator((double)lhs, (double)(long)rhs, numericOperatorNode.op);
			}
			else if (lhs.Type == JTokenType.Integer && rhs.Type == JTokenType.Float)
			{
				return evalDoubleOperator((double)(long)lhs, (double)rhs, numericOperatorNode.op);
			}
			else
            {
				throw new ErrBadNumericArguments(lhs, rhs, numericOperatorNode);
			}
		}

        private static JToken evalIntOperator(long lhs, long rhs, NumericOperatorNode.NumericOperator op)
        {
			long result = op switch {
				NumericOperatorNode.NumericOperator.NumericAdd => lhs + rhs,
				NumericOperatorNode.NumericOperator.NumericSubtract => lhs - rhs,
				NumericOperatorNode.NumericOperator.NumericMultiply => lhs * rhs,
				NumericOperatorNode.NumericOperator.NumericDivide => lhs / rhs,
				NumericOperatorNode.NumericOperator.NumericModulo => lhs % rhs,
				_ => throw new ArgumentException($"Unexpected operator '{op}'")
			};
			return new JValue(result);
        }

		private static JToken evalDoubleOperator(double lhs, double rhs, NumericOperatorNode.NumericOperator op)
		{
			double result = op switch {
				NumericOperatorNode.NumericOperator.NumericAdd => lhs + rhs,
				NumericOperatorNode.NumericOperator.NumericSubtract => lhs - rhs,
				NumericOperatorNode.NumericOperator.NumericMultiply => lhs * rhs,
				NumericOperatorNode.NumericOperator.NumericDivide => lhs / rhs,
				NumericOperatorNode.NumericOperator.NumericModulo => lhs % rhs,
				_ => throw new ArgumentException($"Unexpected operator '{op}'")
			};
			return new JValue(result);
		}

		private static JToken evalNull(NullNode nullNode, JToken input, Environment env)
        {
			return JValue.CreateNull();
        }

        private static JToken evalBoolean(BooleanNode booleanNode, JToken input, Environment env)
        {
			return new JValue(booleanNode.value);
        }

        private static JToken evalString(StringNode stringNode, JToken input, Environment env)
        {
			return JValue.CreateString(stringNode.value);
        }

        private static JToken evalNumber(NumberDoubleNode numberNode, JToken input, Environment env)
        {
			return new JValue(numberNode.value);
        }

		private static JToken evalNumber(NumberIntNode numberNode, JToken input, Environment env)
		{
			return new JValue(numberNode.value);
		}

		private static JToken evalArray(ArrayNode arrayNode, JToken input, Environment env)
        {
			Sequence result = new Sequence() {
				keepSingletons = true
			};
			foreach (Node node in arrayNode.items)
            {
				JToken res = Eval(node, input, env);
				switch (res.Type)
                {
				case JTokenType.Undefined:
					break;
				case JTokenType.Array:
					result.AddRange(res.Children());
					break;
				default:
					result.Add(res);
					break;
				}
			}
			return result;
        }

        private static JToken evalDescendent(DescendentNode descendentNode, JToken input, Environment env)
        {
			Sequence result = new Sequence();
			if (input.Type != JTokenType.Undefined)
			{
				recurseDescendents(result, input);
			}
			return result.Simplify();
		}

        private static void recurseDescendents(Sequence result, JToken input)
        {
            switch (input.Type)
            {
			case JTokenType.Array:
				foreach (JToken child in input.Children())
                {
					recurseDescendents(result, child);
                }
				break;
			case JTokenType.Object:
				result.Add(input);
				foreach (JToken child in ((JObject)input).PropertyValues())
				{
					recurseDescendents(result, child);
				}
				break;
			default:
				result.Add(input);
				break;
			}
        }

        private static JToken evalWildcard(WildcardNode wildcardNode, JToken input, Environment env)
        {
			Sequence result = new Sequence();
			if (input is JArray array 
				&& (input is Sequence) //TODO:???
				&& array.Count > 0
			)
            {
				input = array[0];
            }

            if (input is JObject obj)
            {
				foreach (JToken value in obj.PropertyValues())
                {
					result.AddRange(flattenArray(value));
                }
            }
			return result;
        }

		
		private static IEnumerable<JToken> flattenArray(JToken input)
        {
			switch (input.Type)
            {
			case JTokenType.Array:
				foreach (JToken child in input.Children())
                {
					foreach (JToken result in flattenArray(child))
                    {
						if (result.Type != JTokenType.Undefined)
                        {
							yield return result;
                        }
                    }
                }
				break;
			case JTokenType.Undefined:
				break;
			default:
				yield return input;
				break;
			}
        }

		/*
		private void appendWildcard(Sequence seq, object v)
		{
			switch (v) 
			{
			case JArray jArray:
				break;

			case jtypes.IsArray(v):
				v = flattenArray(v)
				for i, N := 0, v.Len(); i < N; i++ {
					if vi := v.Index(i); vi.IsValid() && vi.CanInterface() {
						seq.Append(vi.Interface())
					}
				}
			default:
				if v.IsValid() && v.CanInterface() {
					seq.Append(v.Interface())
				}
			}
		}
		*/

		private static JToken evalName(NameNode nameNode, JToken data, Environment env)
        {
            switch (data)
            {
			case JObject obj:
				if (!obj.TryGetValue(nameNode.value, out JToken? result))
                {
					return EvalProcessor.UNDEFINED;
                }
				return result;
			case JArray array:
				return evalNameArray(nameNode, array, env);
			default:
				return EvalProcessor.UNDEFINED;
			}
        }

        private static JToken evalNameArray(NameNode nameNode, JArray array, Environment env)
        {
			Sequence result = new Sequence();
			foreach (JToken obj in array.Children())
            {
				JToken res = evalName(nameNode, obj, env);
				if (res.Type != JTokenType.Undefined)
                {
					result.Add(res);
                }
            }
			return result.Simplify();
		}

        private static JToken evalPath(PathNode node, JToken data, Environment env)
		{
			if (node.steps.Count == 0)
			{
				return EvalProcessor.UNDEFINED;
			}

			// expr is an array of steps
			// if the first step is a variable reference ($...), including root reference ($$),
			//   then the path is absolute rather than relative

			bool isVar;
			switch (node.steps[0])
			{
			case VariableNode:
				isVar = true;
				break;
			case PredicateNode predicateNode:
				isVar = predicateNode.expr is VariableNode;
				break;
			default:
				isVar = false;
				break;
			}

			JToken output = data;
			if (isVar || !(data is JArray || data is Sequence))
			{
				//output = new JArray() { data };
				Sequence sequence = new Sequence();
				sequence.Add(data);
				output = sequence;
			};

			int lastIndex = node.steps.Count - 1;
			for (int i = 0; i < node.steps.Count; ++i)
			{
				Node step = node.steps[i];
				// if the first step is an explicit array constructor, then just evaluate that (i.e. don't iterate over a context array)
				if (i == 0 && step is ArrayNode arrayStepNode)
				{
					output = Eval(arrayStepNode, output, env);
				}
				else
				{
					output = evalPathStep(step, output, env, i == lastIndex);
				};

				if (output.Type == JTokenType.Undefined)
				{
					return output;
				}

				if (output is JArray jArray)
				{
					if (jArray.Count == 0)
					{
						return EvalProcessor.UNDEFINED;
					}
				}
			}

			

			if (output is Sequence seq)
			{
				if (node.keepArrays)
				{
					seq.keepSingletons = true;
				}
				return seq.Simplify();
			}
			else
			{
				return output;
			}
		}
		
		private static List<JToken> evalOverArray(Node node, JArray array, Environment env)
		{
			List<JToken> result = new List<JToken>(array.Count);
			foreach (JToken obj in array.Children())
			{
				JToken res = Eval(node, obj, env);
				if (res.Type != JTokenType.Undefined)
				{
					result.Add(res);
				}
			}
			return result;
		}


		private static JToken evalPathStep(Node step, JToken data, Environment env, bool lastStep)
		{
			List<JToken> results;
			if (data is JArray array)
			{
				results = evalOverArray(step, array, env);
			}
			else
            {
				throw new Exception("Not an array or sequence");
            }

			if (lastStep 
				&& results.Count == 1 
				&& (results[0] is JArray)
			)
			{
				return results[0];
			}

			Sequence resultSequence = new Sequence();
			bool isArrayConstructor = step is ArrayNode;
			foreach (JToken v in results)
			{
				//TODO: check http://docs.jsonata.org/processing#sequences
				if (v.Type == JTokenType.Undefined)
				{
					continue;
				}
				else if (isArrayConstructor)
				{
					resultSequence.Add(v);
				}
				else if (v is JArray jarray)
				{
					resultSequence.AddRange(jarray.Children());
				}
				else
				{
					resultSequence.Add(v);
				}
			}

			//???
			//return resultSequence.GetValue();
			/// or maybe
			if (resultSequence.Count == 0)
            {
				return EvalProcessor.UNDEFINED;
            }
			return resultSequence;
		}

        
    }
}

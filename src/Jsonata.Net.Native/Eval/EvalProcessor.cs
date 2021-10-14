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
			case NegationNode negationNode:
				return evalNegation(negationNode, input, env);
			/*
			case RangeNode:
				return evalRange(node, input, env);
			*/
			case ArrayNode arrayNode:
				return evalArray(arrayNode, input, env);
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
			case GroupNode groupNode:
				return evalGroup(groupNode, input, env);
			/*
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
			case ComparisonOperatorNode comparisonOperatorNode:
				return evalComparisonOperator(comparisonOperatorNode, input, env);
			case BooleanOperatorNode booleanOperatorNode:
				return evalBooleanOperator(booleanOperatorNode, input, env);
			/*
			case StringConcatenationNode:
				return evalStringConcatenation(node, input, env);
			*/
			default:
				throw new Exception($"eval: unexpected node type {node.GetType().Name}: {node}");
			}
		}

		private static JToken evalComparisonOperator(ComparisonOperatorNode comparisonOperatorNode, JToken input, Environment env)
		{
			JToken lhs = Eval(comparisonOperatorNode.lhs, input, env);
			JToken rhs = Eval(comparisonOperatorNode.rhs, input, env);
			if (lhs.Type == JTokenType.Undefined || rhs.Type == JTokenType.Undefined)
			{
				switch (comparisonOperatorNode.op)
				{
				case ComparisonOperatorNode.ComparisonOperator.ComparisonEqual:
				case ComparisonOperatorNode.ComparisonOperator.ComparisonNotEqual:
				case ComparisonOperatorNode.ComparisonOperator.ComparisonIn:
					return new JValue(false);
				default:
					return EvalProcessor.UNDEFINED;
				}
			};

			if (lhs.Type == JTokenType.Integer && rhs.Type == JTokenType.Float)
			{
				lhs = new JValue((double)(int)lhs);
			}
			else if (rhs.Type == JTokenType.Integer && lhs.Type == JTokenType.Float)
			{
				rhs = new JValue((double)(int)rhs);
			};

			switch (comparisonOperatorNode.op)
			{
			case ComparisonOperatorNode.ComparisonOperator.ComparisonEqual:
				return JToken.DeepEquals(lhs, rhs);
			case ComparisonOperatorNode.ComparisonOperator.ComparisonNotEqual:
				return !JToken.DeepEquals(lhs, rhs);
			case ComparisonOperatorNode.ComparisonOperator.ComparisonIn:
				{
					if (rhs.Type == JTokenType.Array)
					{
						JArray rhsArray = (JArray)rhs;
						foreach (JToken rhsSubtoken in rhsArray.Children())
						{
							if (JToken.DeepEquals(lhs, rhsSubtoken))
							{
								return true;
							}
						}
						return false;
					}
					else
					{
						return JToken.DeepEquals(lhs, rhs);
					}
				}
			default:
                {
					if (!IsComparable(lhs))
                    {
						throw new JsonataException("T2010", $"Argument '{lhs}' of comparison is not comparable");
                    }
					else if (!IsComparable(rhs))
                    {
						throw new JsonataException("T2010", $"Argument '{rhs}' of comparison is not comparable");
					}
					else if (lhs.Type != rhs.Type)
                    {
						throw new JsonataException("T2009", $"Arguments '{lhs}' and '{rhs}' of comparison are of different types");
					};

					if (lhs.Type == JTokenType.String)
                    {
						return CompareStrings(comparisonOperatorNode.op, (string)lhs!, (string)rhs!);
                    }
					else if (lhs.Type == JTokenType.Integer)
					{
						return CompareInts(comparisonOperatorNode.op, (long)lhs, (long)rhs);
					}
					else if (lhs.Type == JTokenType.Float)
					{
						return CompareDoubles(comparisonOperatorNode.op, (double)lhs, (double)rhs);
					}
					else
                    {
						throw new Exception("Should not happen");
                    }
				}
			}

			bool IsComparable(JToken token)
            {
				return token.Type == JTokenType.Integer
					|| token.Type == JTokenType.Float
					|| token.Type == JTokenType.String;
			}

			JToken CompareDoubles(ComparisonOperatorNode.ComparisonOperator op, double lhs, double rhs)
			{
				switch (op)
                {
				case ComparisonOperatorNode.ComparisonOperator.ComparisonLess:
					return lhs < rhs;
				case ComparisonOperatorNode.ComparisonOperator.ComparisonLessEqual:
					return lhs <= rhs;
				case ComparisonOperatorNode.ComparisonOperator.ComparisonGreater:
					return lhs > rhs;
				case ComparisonOperatorNode.ComparisonOperator.ComparisonGreaterEqual:
					return lhs >= rhs;
				default:
					throw new Exception("Should not happen");
				}
			}

			JToken CompareInts(ComparisonOperatorNode.ComparisonOperator op, long lhs, long rhs)
			{
				switch (op)
				{
				case ComparisonOperatorNode.ComparisonOperator.ComparisonLess:
					return lhs < rhs;
				case ComparisonOperatorNode.ComparisonOperator.ComparisonLessEqual:
					return lhs <= rhs;
				case ComparisonOperatorNode.ComparisonOperator.ComparisonGreater:
					return lhs > rhs;
				case ComparisonOperatorNode.ComparisonOperator.ComparisonGreaterEqual:
					return lhs >= rhs;
				default:
					throw new Exception("Should not happen");
				}
			}

			JToken CompareStrings(ComparisonOperatorNode.ComparisonOperator op, string lhs, string rhs)
			{
				switch (op)
				{
				case ComparisonOperatorNode.ComparisonOperator.ComparisonLess:
					return String.CompareOrdinal(lhs, rhs) < 0;
				case ComparisonOperatorNode.ComparisonOperator.ComparisonLessEqual:
					return String.CompareOrdinal(lhs, rhs) <= 0;
				case ComparisonOperatorNode.ComparisonOperator.ComparisonGreater:
					return String.CompareOrdinal(lhs, rhs) > 0;
				case ComparisonOperatorNode.ComparisonOperator.ComparisonGreaterEqual:
					return String.CompareOrdinal(lhs, rhs) >= 0;
				default:
					throw new Exception("Should not happen");
				}
			}
		}

        private static JToken evalBooleanOperator(BooleanOperatorNode booleanOperatorNode, JToken input, Environment env)
        {
			bool lhs = boolean(Eval(booleanOperatorNode.lhs, input, env)) ?? false; //here undefined works as false? see boolize() in jsonata-js
			bool rhs = boolean(Eval(booleanOperatorNode.rhs, input, env)) ?? false;

			bool result = booleanOperatorNode.op switch {
				BooleanOperatorNode.BooleanOperator.BooleanAnd => lhs && rhs,
				BooleanOperatorNode.BooleanOperator.BooleanOr => lhs || rhs,
				_ => throw new ArgumentException($"Unexpected operator '{booleanOperatorNode.op}'")
			};
			return new JValue(result);
		}

		//null for undefined
		private static bool? boolean(JToken value)
        {
			// cast arg to its effective boolean value
			// boolean: unchanged
			// string: zero-length -> false; otherwise -> true
			// number: 0 -> false; otherwise -> true
			// null -> false
			// array: empty -> false; length > 1 -> true
			// object: empty -> false; non-empty -> true
			// function -> false

			switch (value.Type)
            {
			case JTokenType.Undefined:
				return null;
			case JTokenType.Array:
                {
					JArray array = (JArray)value;
					if (array.Count == 0)
                    {
						return false;
                    }
					else if (array.Count == 1)
                    {
						return boolean(array.Children().First());
					}
                    else
                    {
						return array.Children().Any(c => boolean(c) == true);
                    }
                };
			case JTokenType.String:
				return ((string)value!).Length > 0;
			case JTokenType.Integer:
				return ((long)value) != 0;
			case JTokenType.Float:
				return ((double)value) != 0.0;
			case JTokenType.Object:
				return ((JObject)value!).Count > 0;
			case JTokenType.Boolean:
				return (bool)value;
			default:
				return false;
			}
        }


		private static JToken evalGroup(GroupNode groupNode, JToken input, Environment env)
        {
			JToken items = Eval(groupNode.expr, input, env);
			return evalObject(groupNode.objectNode, items, env);
        }

        private sealed class KeyIndex
        {
			internal readonly int pairIndex;
			internal readonly Sequence inputs = new Sequence();

			internal KeyIndex(int pairIndex, JToken firstInput)
            {
				this.pairIndex = pairIndex;
				this.inputs.Add(firstInput);
            }
        }

        private static JToken evalObject(ObjectNode objectNode, JToken input, Environment env)
        {
			JArray inputArray;
			if (input.Type == JTokenType.Array)
            {
				inputArray = (JArray)input;
            }
			else
            {
				Sequence inputSequence = new Sequence();
				inputSequence.Add(input);
				inputArray = inputSequence;
			};
			
			// if the array is empty, add an undefined entry to enable literal JSON object to be generated
			if (inputArray.Count == 0)
			{
				inputArray.Add(EvalProcessor.UNDEFINED);
			}


			Dictionary<string, KeyIndex> itemsGroupedByKey = new Dictionary<string, KeyIndex>();

			foreach (JToken item in inputArray.Children())
			{
				for (int pairIndex = 0; pairIndex < objectNode.pairs.Count; ++pairIndex)
				{
					Node keyNode = objectNode.pairs[pairIndex].Item1;
					JToken keyToken = Eval(keyNode, item, env);
					if (keyToken.Type != JTokenType.String)
					{
						throw new JsonataException("T1003", $"Object key should be String. Expression evaluated to {keyToken.Type} '{keyToken}'");
					};
					string key = (string)keyToken!;
					if (itemsGroupedByKey.TryGetValue(key, out KeyIndex? keyIndex))
                    {
						if (keyIndex.pairIndex != pairIndex)
                        {
							// this key has been generated by another expression in this group
							// when multiple key expressions evaluate to the same key, then error D1009 must be thrown
							throw new JsonataException("D1009", $"Duplicate object key '{key}'");
						}
						else
                        {
							keyIndex.inputs.Add(item);
                        }
					}
					else
                    {
						itemsGroupedByKey.Add(key, new KeyIndex(pairIndex, item));
					}
				}
			}

			JObject result = new JObject();
			// iterate over the groups to evaluate the 'value' expression
			foreach (KeyValuePair<string, KeyIndex> keyPair in itemsGroupedByKey)
            {
				string key = keyPair.Key;
				Node rhs = objectNode.pairs[keyPair.Value.pairIndex].Item2;
				JToken context = keyPair.Value.inputs;
				JToken value = Eval(rhs, context, env);
				if (value.Type != JTokenType.Undefined)
                {
					result.Add(key, value);
                }
			}
			return result;
        }

        private static JToken evalNegation(NegationNode negationNode, JToken input, Environment env)
        {
			JToken rhs = Eval(negationNode.rhs, input, env);
			switch (rhs.Type)
			{
			case JTokenType.Undefined:
				return EvalProcessor.UNDEFINED;
			case JTokenType.Integer:
				return new JValue(-(long)rhs);
			case JTokenType.Float:
				return new JValue(-(double)rhs);
			default:
				throw new Exception($"Failed to evaluate a non-number {rhs} for a negation");
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
				if (numericOperatorNode.op == NumericOperatorNode.NumericOperator.NumericDivide)
				{
					//divide is still in double
					return evalDoubleOperator((long)lhs, (long)rhs, numericOperatorNode.op);
				}
				else
				{
					return evalIntOperator((long)lhs, (long)rhs, numericOperatorNode.op);
				}
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

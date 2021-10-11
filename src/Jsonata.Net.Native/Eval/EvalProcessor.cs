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

		internal static JToken EvaluateJson(Node rootNode, JToken data)
		{
			//TODO: prepare environment
			Environment environment = new Environment();
			object dataObj = data;
			if (data is JArray)
            {
				dataObj = new Sequence(data) { keepSingletons = true };
            }
			object result = Eval(rootNode, data, environment);
			if (result is Sequence seq)
            {
				//result = seq.GetValue();
				if (seq.values.Count == 1 && !seq.keepSingletons)
				{
					result = seq.values[0];
				}
			}
			return ToJson(result);
		}

		private static JToken ToJson(object result)
		{
			return JToken.FromObject(result);
		}

		internal static object Eval(Node node, object input, Environment env)
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
			case ObjectNode:
				return evalObject(node, input, env);
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
			case NumericOperatorNode:
				return evalNumericOperator(node, input, env);
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

        private static object evalNull(NullNode nullNode, object input, Environment env)
        {
			return JValue.CreateNull();
        }

        private static object evalBoolean(BooleanNode booleanNode, object input, Environment env)
        {
			//todo: think of JValue.Create*
			return booleanNode.value;
        }

        private static object evalString(StringNode stringNode, object input, Environment env)
        {
			//todo: think of JValue.Create*
			return stringNode.value;
        }

        private static object evalNumber(NumberDoubleNode numberNode, object input, Environment env)
        {
			//todo: think of JValue.Create*
			return numberNode.value;
        }

		private static object evalNumber(NumberIntNode numberNode, object input, Environment env)
		{
			//todo: think of JValue.Create*
			return numberNode.value;
		}

		private static object evalArray(ArrayNode arrayNode, object input, Environment env)
        {
			Sequence result = new Sequence(new List<object>(arrayNode.items.Count)) {
				keepSingletons = true
			};
			foreach (Node node in arrayNode.items)
            {
				object res = Eval(node, input, env);
				switch (res)
                {
				case Undefined:
					break;
				case Sequence sequence:
					result.values.AddRange(sequence.values);
					break;
				case JArray array:
					result.values.AddRange(array.Values());
					break;
				default:
					result.values.Add(res);
					break;
				}
			}
			return result;
        }

        private static object evalDescendent(DescendentNode descendentNode, object input, Environment env)
        {
			Sequence result = new Sequence(new List<object>());
			if (input != Undefined.Instance)
			{
				recurseDescendents(result, input);
			}
			return result.GetValue();
		}

        private static void recurseDescendents(Sequence result, object v)
        {
            switch (v)
            {
			case JArray array:
				foreach (JToken child in array)
                {
					recurseDescendents(result, child);
                }
				break;
			case Sequence sequence:
				foreach (object child in sequence.values)
				{
					recurseDescendents(result, child);
				}
				break;
			case JObject obj:
				result.values.Add(obj);
				foreach (JToken child in obj.PropertyValues())
				{
					recurseDescendents(result, child);
				}
				break;
			default:
				result.values.Add(v);
				break;
			}
        }

        private static object evalWildcard(WildcardNode wildcardNode, object input, Environment env)
        {
			Sequence result = new Sequence(new List<object>());
			if (input is JArray array && array.Count > 0)
            {
				input = array[0];
            }

            if (input is JObject obj)
            {
				foreach (JToken value in obj.PropertyValues())
                {
					result.values.AddRange(flattenArray(value));
                }
            }
			return result;
        }

		
		private static IEnumerable<object> flattenArray(object v)
        {
			switch (v)
            {
			case JArray array:
				foreach (JToken child in array)
                {
					foreach (object result in flattenArray(child))
                    {
						if (result != Undefined.Instance)
                        {
							yield return result;
                        }
                    }
                }
				break;
			case Sequence sequence:
				foreach (object child in sequence.values)
				{
					foreach (object result in flattenArray(child))
					{
						if (result != Undefined.Instance)
						{
							yield return result;
						}
					}
				}
				break;
			case Undefined:
				break;
			default:
				yield return v;
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

		private static object evalName(NameNode nameNode, object data, Environment env)
        {
            switch (data)
            {
			case JObject obj:
				if (!obj.TryGetValue(nameNode.value, out JToken? result))
                {
					return Undefined.Instance;
                }
				return result;
			case JArray array:
				return evalNameArray(nameNode, array, env);
			default:
				return Undefined.Instance;
			}
        }

        private static object evalNameArray(NameNode nameNode, JArray array, Environment env)
        {
			Sequence result = new Sequence(array.Count);
			foreach (JToken obj in array)
            {
				object res = evalName(nameNode, obj, env);
				if (res != Undefined.Instance)
                {
					result.values.Add(res);
                }
            }
			return result.GetValue();
		}

        private static object evalPath(PathNode node, object data, Environment env)
		{
			if (node.steps.Count == 0)
			{
				return Undefined.Instance;
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

			object output = data;
			if (isVar || !(data is JArray || data is Sequence))
			{
				//output = new JArray() { data };
				output = new Sequence(data);
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

				if (output == Undefined.Instance)
				{
					return output;
				}

				if (output is JArray jArray)
				{
					if (jArray.Count == 0)
					{
						return Undefined.Instance;
					}
				}
				if (output is Sequence sequence)
				{
					if (sequence.values.Count == 0)
					{
						return Undefined.Instance;
					}
				}
			}

			if (node.keepArrays)
			{
				if (output is Sequence sequence)
				{
					sequence.keepSingletons = true;
				}
			}

			return output;
		}
		
		private static List<object> evalOverSequence(Node node, Sequence seq, Environment env)
		{
			List<object> result = new List<object>(seq.values.Count);
			foreach (object obj in seq.values)
            {
				object res = Eval(node, obj, env);
				if (res != Undefined.Instance)
                {
					result.Add(res);
                }
            }
			return result;
		}

		private static List<object> evalOverArray(Node node, JArray array, Environment env)
		{
			List<object> result = new List<object>(array.Count);
			foreach (object obj in array)
			{
				object res = Eval(node, obj, env);
				if (res != Undefined.Instance)
				{
					result.Add(res);
				}
			}
			return result;
		}


		private static object evalPathStep(Node step, object data, Environment env, bool lastStep)
		{
			List<object> results;
			if (data is Sequence sequence)
			{
				results = evalOverSequence(step, sequence, env);
			}
			else if (data is JArray array)
			{
				results = evalOverArray(step, array, env);
			}
			else
            {
				throw new Exception("Not an array or sequence");
            }

			if (lastStep && results.Count == 1 && (results[0] is JArray || results[0] is Sequence))
			{
				return results[0];
			}

			Sequence resultSequence = new Sequence(new List<object>());
			bool isArrayConstructor = step is ArrayNode;
			foreach (object v in results)
			{
				//TODO: check http://docs.jsonata.org/processing#sequences
				if (v == Undefined.Instance)
				{
					continue;
				}
				else if (isArrayConstructor)
				{
					resultSequence.values.Add(v);
				}
				else if (v is JArray jarray)
				{
					resultSequence.values.AddRange(jarray.Children());
				}
				else if (v is Sequence vSeq)
				{
					resultSequence.values.AddRange(vSeq.values);
				}
				else
				{
					resultSequence.values.Add(v);
				}
			}

			//???
			//return resultSequence.GetValue();
			/// or maybe
			if (resultSequence.values.Count == 0)
            {
				return Undefined.Instance;
            }
			return resultSequence;
		}

        
    }
}

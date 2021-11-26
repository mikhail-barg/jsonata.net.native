using Jsonata.Net.Native.Parsing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
	internal static class EvalProcessor
	{
		internal static readonly JValue UNDEFINED = JValue.CreateUndefined();

		internal static JToken EvaluateJson(Node rootNode, JToken data, JObject? bindings)
		{
			Environment environment = Environment.CreateEvalEnvironment();
			//TODO: add default bindings
			if (bindings != null)
            {
				foreach (JProperty property in bindings.Properties())
                {
					environment.Bind(property.Name, property.Value);
                }
            };
			// put the input document into the environment as the root object
			environment.Bind("$", data);

			if (data.Type == JTokenType.Array)
            {
				// if the input is a JSON array, then wrap it in a singleton sequence so it gets treated as a single input
				JArray dataArr = new Sequence() { outerWrapper = true };
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
			JToken result = EvalInternal(node, input, env);
			if (result is Sequence sequence)
            {
				if (!sequence.HasValues)
                {
					return EvalProcessor.UNDEFINED;
                }
				else if (sequence.Count == 1 && !sequence.keepSingletons)
                {
					return sequence[0];
                }
            };
			return result;
		}

		private static JToken EvalInternal(Node node, JToken input, Environment env)
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
			case RegexNode regexNode:
				return evalRegex(regexNode, input, env);
			case VariableNode variableNode:
				return evalVariable(variableNode, input, env);
			case NameNode nameNode:
				return evalName(nameNode, input, env);
			case PathNode pathNode:
				return evalPath(pathNode, input, env);
			case NegationNode negationNode:
				return evalNegation(negationNode, input, env);
			case RangeNode rangeNode:
				return evalRange(rangeNode, input, env);
			case ArrayNode arrayNode:
				return evalArray(arrayNode, input, env);
			case ObjectNode objectNode:
				return evalObject(objectNode, input, env);
			case BlockNode blockNode:
				return evalBlock(blockNode, input, env);
			case ConditionalNode conditionalNode:
				return evalConditional(conditionalNode, input, env);
			case AssignmentNode assignmentNode:
				return evalAssignment(assignmentNode, input, env);
			case WildcardNode wildcardNode:
				return evalWildcard(wildcardNode, input, env);
			case DescendentNode descendentNode:
				return evalDescendent(descendentNode, input, env);
			case GroupNode groupNode:
				return evalGroup(groupNode, input, env);
			case PredicateNode predicateNode:
				return evalPredicate(predicateNode, input, env);
			case SortNode sortNode:
				return evalSort(sortNode, input, env);
			case LambdaNode lambdaNode:
				return evalLambda(lambdaNode, input, env);
			case ObjectTransformationNode transformationNode:
				return evalObjectTransformation(transformationNode, input, env);
			case PartialNode partialNode:
				return evalPartial(partialNode, input, env);
			case FunctionCallNode functionCallNode:
				return evalFunctionCall(functionCallNode, input, env, null);
			case FunctionApplicationNode functionApplicationNode:
				return evalFunctionApplication(functionApplicationNode, input, env);
			case NumericOperatorNode numericOperatorNode:
				return evalNumericOperator(numericOperatorNode, input, env);
			case ComparisonOperatorNode comparisonOperatorNode:
				return evalComparisonOperator(comparisonOperatorNode, input, env);
			case BooleanOperatorNode booleanOperatorNode:
				return evalBooleanOperator(booleanOperatorNode, input, env);
			case StringConcatenationNode stringConcatenationNode:
				return evalStringConcatenation(stringConcatenationNode, input, env);
			default:
				throw new NotImplementedException($"eval: unexpected node type {node.GetType().Name}: {node}");
			}
		}

        private static JToken evalRegex(RegexNode regexNode, JToken input, Environment env)
        {
			return new FunctionTokenRegex(regexNode.regex);
        }

        private static JToken evalSort(SortNode sortNode, JToken input, Environment env)
        {
			JToken items = EvalProcessor.Eval(sortNode.expr, input, env);
			switch (items.Type)
            {
			case JTokenType.Undefined:
				return EvalProcessor.UNDEFINED;
			case JTokenType.Array:
				break;
			default:
				return items;
            }
			List<JToken> itemsList = items.Children().ToList();

			try
			{
				itemsList.Sort(comparison);
			}
			catch (InvalidOperationException ex)
            {
				if (ex.InnerException != null)
				{
					ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				}
				else
                {
					throw;
                }
			}

			JArray result = new JArray();
			foreach (JToken item in itemsList)
            {
				result.Add(item);
            }
			return result;

			int comparison(JToken a, JToken b)
			{
				foreach (SortNode.Term term in sortNode.terms)
				{
					//evaluate the sort term in the context of a
					JToken aa = EvalProcessor.Eval(term.expr, a, env);
					//evaluate the sort term in the context of b
					JToken bb = EvalProcessor.Eval(term.expr, b, env);

					// undefined should be last in sort order
					if (aa.Type == JTokenType.Undefined)
                    {
						if (bb.Type == JTokenType.Undefined)
						{
							continue;
						}
						else
                        {
							return 1;
                        }
                    }
					else if (bb.Type == JTokenType.Undefined)
                    {
						return -1;
                    }

					if (aa.Type != JTokenType.String && aa.Type != JTokenType.Integer && aa.Type != JTokenType.Float)
                    {
						throw new JsonataException("T2008", $"The expressions within an order-by clause must evaluate to numeric or string values. Got {aa.Type} ({aa.ToString(Newtonsoft.Json.Formatting.None)})");
                    };
					if (bb.Type != JTokenType.String && bb.Type != JTokenType.Integer && bb.Type != JTokenType.Float)
					{
						throw new JsonataException("T2008", $"The expressions within an order-by clause must evaluate to numeric or string values. Got {bb.Type} ({bb.ToString(Newtonsoft.Json.Formatting.None)})");
					};

					if ((aa.Type == JTokenType.String) != (bb.Type == JTokenType.String))
                    {
						throw new JsonataException("T2007", $"Type mismatch when comparing values {aa.Type}({aa.ToString(Newtonsoft.Json.Formatting.None)}) and {bb.Type}({bb.ToString(Newtonsoft.Json.Formatting.None)}) in order-by clause");
                    }

					int comp;

					if (aa.Type == JTokenType.String)
                    {
						comp = String.Compare((string)aa!, (string)bb!);
                    }
					else
                    {
						double aValue = aa.Type == JTokenType.Float ? (double)aa : (long)aa;
						double bValue = bb.Type == JTokenType.Float ? (double)bb : (long)bb;
						comp = aValue.CompareTo(bValue);
					};

					if (term.dir == SortNode.Direction.Descending)
					{
						comp = -comp;
					};

					if (comp != 0)
					{
						return comp;
					}
				};
				return 0;
			}
		}

		private static JToken evalObjectTransformation(ObjectTransformationNode transformationNode, JToken input, Environment env)
        {
			return new FunctionTokenTransformation(
				pattern: transformationNode.pattern,
				updates: transformationNode.updates,
				deletes: transformationNode.deletes,
				environment: env
			);
        }

        private static JToken evalPartial(PartialNode partialNode, JToken input, Environment env)
        {
			JToken func = Eval(partialNode.func, input, env);

			if (func is not FunctionToken function)
			{
				throw new JsonataException("T1008", $"Attempted to partially apply a non-function '{func.ToString(Newtonsoft.Json.Formatting.None)}' got from '{partialNode.func}'");
			};

			List<JToken?> argsOrNulls = new List<JToken?>(partialNode.args.Count);
			foreach (Node argNode in partialNode.args)
            {
				if (argNode is PlaceholderNode)
                {
					argsOrNulls.Add(null);
                }
				else
                {
					JToken arg = Eval(argNode, input, env);
					argsOrNulls.Add(arg);
                }
            }
			return new FunctionTokenPartial(function, argsOrNulls);
		}

        private static JToken evalLambda(LambdaNode lambdaNode, JToken input, Environment env)
        {
			return new FunctionTokenLambda(
				signature: lambdaNode.signature,
				paramNames: lambdaNode.paramNames,
				body: lambdaNode.body,
				context: input,
				environment: env
			);
		}

        private static JToken evalConditional(ConditionalNode conditionalNode, JToken input, Environment env)
        {
			JToken condition = Eval(conditionalNode.predicate, input, env);
			if (Helpers.Booleanize(condition))
            {
				return Eval(conditionalNode.expr1, input, env);
            }
			else if (conditionalNode.expr2 != null)
            {
				return Eval(conditionalNode.expr2, input, env);
			}
            else
            {
				return EvalProcessor.UNDEFINED;
            }
        }

        private static JToken evalFunctionApplication(FunctionApplicationNode functionApplicationNode, JToken input, Environment env)
        {
			JToken lhs = Eval(functionApplicationNode.lhs, input, env);
			if (functionApplicationNode.rhs is FunctionCallNode functionCallNode)
            {
				// this is a function _invocation_; invoke it with lhs expression as the first argument
				return evalFunctionCall(functionCallNode, input, env, evalutedFirstArgFromApplication: lhs);
			}
			else
            {
				JToken rhs = Eval(functionApplicationNode.rhs, input, env);
				if (rhs.Type != FunctionToken.TYPE)
                {
					throw new JsonataException("T2006", "The right side of the function application operator ~> must be a function");
                };
				if (lhs.Type == FunctionToken.TYPE)
                {
					// this is function chaining (func1 ~> func2)
					// λ($f, $g) { λ($x){ $g($f($x)) } }

					//original jsonata-js used following AST here:
					// var chainAST = parser('function($f, $g) { function($x){ $g($f($x)) } }');
					/* TODO: use pre-compiled AST, or at least parse chainAST just once
					return new FunctionTokenLambda(
						signature: null,
						paramNames: new List<string>() { "x" },
						body: new FunctionCallNode(
							new 
						),
						context: input,
						environment: env
					);
					*/
					JsonataQuery chainAST = new JsonataQuery("function($f, $g) { function($x){ $g($f($x)) } }");
					JToken chain = chainAST.Eval(EvalProcessor.UNDEFINED); //TODO: probably need to provide env as an environment here
					JToken result = InvokeFunction((FunctionToken)chain, new List<JToken>() { lhs, rhs }, null, env);
					return result;
				}
				else
                {
					return InvokeFunction((FunctionToken)rhs, new List<JToken>() { lhs }, null, env);
                }
			}
		}

        private static JToken evalRange(RangeNode rangeNode, JToken input, Environment env)
        {
			JToken lhs = Eval(rangeNode.lhs, input, env);
			JToken rhs = Eval(rangeNode.rhs, input, env);

			if (lhs.Type != JTokenType.Undefined && lhs.Type != JTokenType.Integer)
            {
				throw new JsonataException("T2003", $"The left side of the range operator (..) must evaluate to an integer, got {lhs.Type}");
			}
			else if (rhs.Type != JTokenType.Undefined && rhs.Type != JTokenType.Integer)
			{
				throw new JsonataException("T2004", $"The right side of the range operator (..) must evaluate to an integer, got {rhs.Type}");
			}
			else if (lhs.Type == JTokenType.Undefined || rhs.Type == JTokenType.Undefined)
            {
				// if either side is undefined, the result is undefined
				return EvalProcessor.UNDEFINED;
            };

			long lhsValue = (long)lhs;
			long rhsValue = (long)rhs;

			if (lhsValue > rhsValue)
			{
				// if the lhs is greater than the rhs, return undefined
				return EvalProcessor.UNDEFINED;
			};

			// limit the size of the array to ten million entries (1e7)
			// this is an implementation defined limit to protect against
			// memory and performance issues.  This value may increase in the future.
			long size = rhsValue - lhsValue + 1;
			if (size > 1e7)
			{
				throw new JsonataException("D2014", $"The size of the sequence allocated by the range operator (..) must not exceed 1e7.  Attempted to allocate {size}.");
			};

			JArray result = new Sequence();
			for (long value = lhsValue; value <= rhsValue; ++value)
			{
				result.Add(new JValue(value));
			}
			return result;
		}

        private static JToken evalAssignment(AssignmentNode assignmentNode, JToken input, Environment env)
        {
			JToken value = Eval(assignmentNode.value, input, env);
			env.Bind(assignmentNode.name, value);
			return value;
        }

        private static JToken evalBlock(BlockNode blockNode, JToken input, Environment env)
        {
			// create a new frame to limit the scope of variable assignments
			// TODO, only do this if the post-parse stage has flagged this as required
			Environment localEnvironment = Environment.CreateNestedEnvironment(env);

			// invoke each expression in turn
			// only return the result of the last one
			JToken result = EvalProcessor.UNDEFINED;
			foreach (Node expression in blockNode.expressions)
            {
				result = Eval(expression, input, localEnvironment);
            }
			return result;
		}

        private static JToken evalFunctionCall(FunctionCallNode functionCallNode, JToken input, Environment env, JToken? evalutedFirstArgFromApplication)
        {
            JToken func = Eval(functionCallNode.func, input, env);
            if (func is not FunctionToken function)
            {
                throw new JsonataException("T1006", $"Attempted to invoke a non-function '{func.ToString(Newtonsoft.Json.Formatting.None)}' got from '{functionCallNode.func}'");
            }

            List<JToken> args = new List<JToken>();
            if (evalutedFirstArgFromApplication != null)
            {
                args.Add(evalutedFirstArgFromApplication);
            };
            foreach (Node argNode in functionCallNode.args)
            {
                JToken argValue = Eval(argNode, input, env);
                args.Add(argValue);
            }

            JToken? context = evalutedFirstArgFromApplication != null ? null : input;

            return InvokeFunction(function, args, context, env);
        }

        internal static JToken InvokeFunction(FunctionToken function, List<JToken> args, JToken? context, Environment env)
        {
            JToken result;
            switch (function)
            {
            case FunctionTokenCsharp nativeFunction:
                result = EvalProcessor_Functions.CallCsharpFunction(nativeFunction.functionName, nativeFunction.methodInfo, args, context, env);
                break;
            case FunctionTokenLambda lambdaFunction:
                result = EvalProcessor_Lambda.CallLambdaFunction(lambdaFunction, args, context);
                break;
			case FunctionTokenPartial partialFunction:
				{
					List<JToken> alignedArgs = AlignPartialFunctionArgs(partialFunction, args, context);
					result = InvokeFunction(partialFunction.func, alignedArgs, null, env);
				}
				break;
			case FunctionTokenTransformation transformationFunction:
                {
					result = EvalProcessor_Transformation.CallTransformationFunction(transformationFunction, args, context);
                }
				break;
			case FunctionTokenRegex regexFunction:
				throw new NotImplementedException("TODO: regex");
			default:
                throw new Exception("Unexpected function token type " + function.GetType().Name);
            }
            return result;
        }

        private static List<JToken> AlignPartialFunctionArgs(FunctionTokenPartial partialFunction, List<JToken> args, JToken? context)
        {
			int expectedArgsCount = partialFunction.GetArgumentsCount();
			bool useContext = false;
			if (expectedArgsCount == args.Count + 1 && context != null)
            {
				useContext = true;
            };
			int nextArgIndex = 0;
			List<JToken> result = new List<JToken>(partialFunction.argsOrPlaceholders.Count);
			foreach (JToken? arg in partialFunction.argsOrPlaceholders)
            {
				if (arg == null)
                {
					if (useContext)
                    {
						result.Add(context!);
						useContext = false;
                    }
					else if (nextArgIndex < args.Count)
                    {
						result.Add(args[nextArgIndex]);
						++nextArgIndex;
                    }
					else
                    {
						result.Add(EvalProcessor.UNDEFINED);
                    }
                }
                else
                {
					result.Add(arg);
                }
            };
			return result;
        }

        private static JToken evalVariable(VariableNode variableNode, JToken input, Environment env)
        {
			if (variableNode.name == "")
			{
				return input;
			};
			return env.Lookup(variableNode.name);
		}

        private static JToken evalPredicate(PredicateNode predicateNode, JToken input, Environment env)
        {
			JToken itemsToken = Eval(predicateNode.expr, input, env);
			if (itemsToken.Type == JTokenType.Undefined)
            {
				return EvalProcessor.UNDEFINED;
            };

			JArray itemsArray;
			if (itemsToken.Type == JTokenType.Array)
            {
				itemsArray = (JArray)itemsToken;
            }
            else
            {
				itemsArray = new Sequence();
				itemsArray.Add(itemsToken);
            };

			foreach (Node filter in predicateNode.filters)
            {
				itemsArray = evalFilter(filter, itemsArray, env);
				if (itemsArray.Count == 0)
                {
					return EvalProcessor.UNDEFINED;
                }
            }

			if (itemsArray is Sequence sequence)
            {
				return sequence.Simplify();
            }
			return itemsArray;
        }

        private static JArray evalFilter(Node filter, JArray itemsArray, Environment env)
        {
			if (filter is NumberNode numberNode)
			{
				int index = numberNode.GetIntValue();
				JToken resultToken = GetArrayElementByIndex(itemsArray, index);
				if (resultToken.Type == JTokenType.Array)
				{
					return (JArray)resultToken;
				}
				else
				{
					Sequence result = new Sequence();
					result.Add(resultToken);
					return result;
				}
			}
			else
			{
				Sequence result = new Sequence();
				for (int index = 0; index < itemsArray.Count; ++index)
				{
					JToken item = itemsArray[index];
					JToken res = Eval(filter, item, env);
					if (res.Type == JTokenType.Integer || res.Type == JTokenType.Float)
					{
						CheckAppendToken(result, item, index, res);
					}
					else if (Helpers.IsArrayOfNumbers(res))
					{
						foreach (JToken subtoken in ((JArray)res).Children())
						{
							CheckAppendToken(result, item, index, subtoken);
						}
					}
					else if (Helpers.Booleanize(res))
                    {
						result.Add(item);
                    }
				}
				return result;
			}

			int WrapArrayIndex(JArray array, int index)
			{
				if (index < 0)
				{
					index = array.Count + index;
				};
				return index;
			}

			JToken GetArrayElementByIndex(JArray array, int index)
			{
				index = WrapArrayIndex(array, index);
				if (index < 0 || index >= array.Count)
				{
					return EvalProcessor.UNDEFINED;
				}
				else
				{
					return array[index];
				}
			}

			void CheckAppendToken(Sequence result, JToken item, int itemIndex, JToken indexToken)
            {
				if (indexToken.Type == JTokenType.Integer)
				{
					int indexTokenValue = WrapArrayIndex(itemsArray, (int)(long)indexToken);
					if (indexTokenValue == itemIndex)
					{
						result.Add(item);
					}
				}
				else if (indexToken.Type == JTokenType.Float)
				{
					int indexTokenValue = WrapArrayIndex(itemsArray, (int)(double)indexToken);
					if (indexTokenValue == itemIndex)
					{
						result.Add(item);
					}
				}
			}
        }

        private static JToken evalStringConcatenation(StringConcatenationNode stringConcatenationNode, JToken input, Environment env)
        {
            string lstr = stringify(Eval(stringConcatenationNode.lhs, input, env));
			string rstr = stringify(Eval(stringConcatenationNode.rhs, input, env));
			return lstr + rstr;
		}

        private static string stringify(JToken token)
        {
			switch (token.Type)
            {
			case JTokenType.Undefined:
				return "";
			case JTokenType.String:
				return (string)token!;
			case JTokenType.Array:
                {
					JArray array = (JArray)token;
					if (array is Sequence sequence && !sequence.keepSingletons && sequence.Count == 1)
                    {
						return stringify(array.Children().First());
                    }
					return array.ToString(Newtonsoft.Json.Formatting.None);
                }
			default:
				return token.ToString(Newtonsoft.Json.Formatting.None);
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
					if (lhs.Type != JTokenType.Undefined && !IsComparable(lhs))
					{
						throw new JsonataException("T2010", $"Argument '{lhs}' of comparison is not comparable");
					}
					else if (rhs.Type != JTokenType.Undefined && !IsComparable(rhs))
					{
						throw new JsonataException("T2010", $"Argument '{rhs}' of comparison is not comparable");
					}
					else
					{
						return EvalProcessor.UNDEFINED;
					}
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
			bool lhs = Helpers.Booleanize(Eval(booleanOperatorNode.lhs, input, env)); //here undefined works as false? see boolize() in jsonata-js
			//short-cirquit the operators if possible:
			switch (booleanOperatorNode.op)
            {
			case BooleanOperatorNode.BooleanOperator.BooleanAnd:
				if (!lhs)
                {
					return new JValue(false);
                }
				break;
			case BooleanOperatorNode.BooleanOperator.BooleanOr:
				if (lhs)
				{
					return new JValue(true);
				}
				break;
			};


			bool rhs = Helpers.Booleanize(Eval(booleanOperatorNode.rhs, input, env));

			bool result = booleanOperatorNode.op switch {
				BooleanOperatorNode.BooleanOperator.BooleanAnd => lhs && rhs,
				BooleanOperatorNode.BooleanOperator.BooleanOr => lhs || rhs,
				_ => throw new ArgumentException($"Unexpected operator '{booleanOperatorNode.op}'")
			};
			return new JValue(result);
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
			long longResult = (long)result;
			if (longResult == result)
			{
				return new JValue(longResult);
			}
			else
			{
				return new JValue(result);
			}
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
			JArray result = new ExplicitArray();
			foreach (Node node in arrayNode.items)
            {
				JToken res = Eval(node, input, env);
				switch (res.Type)
                {
				case JTokenType.Undefined:
					break;
				case JTokenType.Array:
					if (node is ArrayNode)
                    {
						result.Add(res);
					}
					else if (res.Type == JTokenType.Array
						&& (!(res is Sequence sequence) || !sequence.keepSingletons)
					)
					{
						result.AddRange(res.Children());
					}
					else
                    {
						result.Add(res);
                    }
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
			if (input is Sequence inputSequence
				&& inputSequence.HasValues
				&& inputSequence.outerWrapper
			)
            {
				input = inputSequence[0];
            }

			Sequence result = new Sequence();
			switch (input.Type)
			{
			case JTokenType.Object:
				JObject obj = (JObject)input;
				foreach (JToken value in obj.PropertyValues())
				{
					result.AddRange(flattenArray(value));
				}
				break;
			case JTokenType.Array:
				foreach (JToken value in input.Children())
				{
					result.AddRange(flattenArray(value));
				}
				break;
			default:
				break;
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

		private static JToken evalName(NameNode nameNode, JToken data, Environment env)
        {
            switch (data)
            {
			case JObject obj:
				{
					if (!obj.TryGetValue(nameNode.value, out JToken? result))
					{
						return EvalProcessor.UNDEFINED;
					}
					return result;
				}
			case JArray array:
                {
					Sequence result = new Sequence();
					foreach (JToken obj in array.Children())
					{
						JToken res = evalName(nameNode, obj, env);
						switch (res.Type)
						{
						case JTokenType.Undefined:
							//ignore
							break;
						case JTokenType.Array:
							result.AddRange(res.Children());
							break;
						default:
							result.Add(res);
							break;
						}
					}
					//return result.Simplify();r
					return result;
				}
			default:
				return EvalProcessor.UNDEFINED;
			}
        }
       
        private static JToken evalPath(PathNode node, JToken data, Environment env)
		{
			if (node.steps.Count == 0)
			{
				return EvalProcessor.UNDEFINED;
			}

			// if the first step is a variable reference ($...), including root reference ($$),
			//   then the path is absolute rather than relative
			bool isVar = node.steps[0] switch {
                VariableNode => true,
                PredicateNode predicateNode => predicateNode.expr is VariableNode,
                _ => false,
            };

			JArray array;
			if (data.Type == JTokenType.Array && !isVar)
            {
				//already an array
				array = (JArray)data;
            }
			else
			{
				// if input is not an array, make it so
				Sequence sequence = new Sequence();
				sequence.Add(data);
				array = sequence;
			};

			int lastIndex = node.steps.Count - 1;
			for (int stepIndex = 0; stepIndex < node.steps.Count; ++stepIndex)
			{
				Node step = node.steps[stepIndex];
				// if the first step is an explicit array constructor, then just evaluate that (i.e. don't iterate over a context array)
				if (stepIndex == 0 && step is ArrayNode arrayStepNode)
				{
					array = (JArray)Eval(arrayStepNode, array, env);
				}
				else
				{
					array = evalPathStep(step, array, env, stepIndex == lastIndex);
				};

				if (!array.HasValues)
                {
					break;
                }
			}

			if (node.keepArrays)
            {
				if (array is Sequence arraySequence)
				{
					arraySequence.keepSingletons = true;
				}
				else if (array is ExplicitArray)
				{
					// if the array is explicitly constructed in the expression and marked to promote singleton sequences to array
					Sequence resultSequence = new Sequence() {
						keepSingletons = true
					};
					resultSequence.Add(array);
					array = resultSequence;
				}
				else
				{
					//this case is not explicitly defined in jsonata-js, because only sequences are expected to have keepSingletons, but still..
					//maybe we'll need to convert current array to sequence to set keepSingletons
				}
			}
			return array;
		}

		private static JArray evalPathStep(Node step, JArray array, Environment env, bool lastStep)
		{
			List<JToken> result = new List<JToken>(array.Count);
			foreach (JToken obj in array.Children())
			{
				JToken resultToken = Eval(step, obj, env);
				if (resultToken.Type != JTokenType.Undefined)
				{
					result.Add(resultToken);
				}
			};

			if (lastStep 
				&& result.Count == 1 
				&& result[0].Type == JTokenType.Array
				&& !(result[0] is Sequence)
			)
			{
				return (JArray)result[0];
			};

			// flatten the sequence
			//see also http://docs.jsonata.org/processing#sequences
			Sequence resultSequence = new Sequence();
			bool isArrayConstructor = step is ArrayNode;
			foreach (JToken resultToken in result)
			{
				if (resultToken.Type != JTokenType.Array   // <=  !Array.isArray(res)
					|| isArrayConstructor				   // <=  res.cons
				)
				{
					// it's not an array - just push into the result sequence
					resultSequence.Add(resultToken);
				}
				else
                {
					// res is a sequence - flatten it into the parent sequence
					resultSequence.AddRange(resultToken.Children());
				}
			}

			return resultSequence;
		}

        
    }
}

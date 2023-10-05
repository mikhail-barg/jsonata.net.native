using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;

namespace Jsonata.Net.Native.Eval
{

	internal sealed class FunctionTokenCsharp : FunctionToken
	{
		private readonly object? m_target;
		private readonly MethodInfo m_methodInfo;
        private IReadOnlyList<ArgumentInfo> m_parameters;
		private readonly string m_functionName;
		private readonly bool m_hasContextParameter;
		private readonly bool m_hasEnvParameter;

        internal FunctionTokenCsharp(string funcName, MethodInfo methodInfo)
			:this(funcName, methodInfo, null)
		{
			if (!methodInfo.IsStatic)
			{
                throw new ArgumentException("Only static methods are allowed to be bound as Jsonata functions via MethodInfo");
            }
		}

        internal FunctionTokenCsharp(string funcName, Delegate delegateFunc)
			: this(funcName, delegateFunc.Method, delegateFunc.Target)
        {
        }

        private FunctionTokenCsharp(string funcName, MethodInfo methodInfo, object? target)
			: base($"{methodInfo.DeclaringType?.Name}.{methodInfo.Name}", methodInfo.GetParameters().Length)
		{
			this.m_functionName = funcName;
			this.m_methodInfo = methodInfo;
			this.m_target = target;
            this.m_parameters = this.m_methodInfo.GetParameters()
				.Select(pi => new ArgumentInfo(funcName, pi))
				.ToList();
			this.m_hasContextParameter = this.m_parameters.Any(p => p.allowContextAsValue);
			this.m_hasEnvParameter = this.m_parameters.Any(p => p.isEvaluationSupplement);

			this.RequiredArgsCount = this.m_parameters.Where(p => !p.isOptional && !p.isEvaluationSupplement).Count();
		}

        internal sealed class ArgumentInfo
		{
			internal readonly string name;
			internal readonly Type parameterType;
			internal readonly bool propagateUndefined;
			internal readonly bool allowContextAsValue;
			internal readonly bool packSingleValueToSequence;
			internal readonly bool isOptional;
			internal readonly object? defaultValueForOptional;
			internal readonly bool isEvaluationSupplement;
			internal readonly bool isVariableArgumentsArray;

			internal ArgumentInfo(string functionName, ParameterInfo parameterInfo)
			{
				this.name = parameterInfo.Name!;
				this.parameterType = parameterInfo.ParameterType;
				this.propagateUndefined = parameterInfo.IsDefined(typeof(PropagateUndefinedAttribute), false);
				this.allowContextAsValue = parameterInfo.IsDefined(typeof(AllowContextAsValueAttribute), false);
				this.packSingleValueToSequence = parameterInfo.IsDefined(typeof(PackSingleValueToSequenceAttribute), false);

				OptionalArgumentAttribute? optionalArgumentAttribute = parameterInfo.GetCustomAttribute<OptionalArgumentAttribute>(false);
				if (optionalArgumentAttribute != null)
				{
					this.isOptional = true;
					this.defaultValueForOptional = optionalArgumentAttribute.DefaultValue;
				}
				else
				{
					this.isOptional = false;
					this.defaultValueForOptional = null;
				};

				this.isEvaluationSupplement = parameterInfo.IsDefined(typeof(EvalSupplementArgumentAttribute), false);
				if (this.isEvaluationSupplement && parameterInfo.ParameterType != typeof(EvaluationSupplement))
				{
					throw new JsonataException("????", $"Declaration error for function '{functionName}': attribute [{nameof(EvalSupplementArgumentAttribute)}] can only be specified for arguments of type {nameof(EvaluationSupplement)}");
				};

				this.isVariableArgumentsArray = parameterInfo.IsDefined(typeof(VariableNumberArgumentAsArrayAttribute), false);
				if (this.isVariableArgumentsArray && parameterInfo.ParameterType != typeof(JArray))
				{
					throw new Exception($"Declaration error for function '{functionName}': attribute [{nameof(VariableNumberArgumentAsArrayAttribute)}] can only be specified for arguments of type {nameof(JArray)}");
				};
			}
		}

		internal override JToken Invoke(List<JToken> args, JToken? context, EvaluationEnvironment env)
		{
			object?[] parameters = this.BindFunctionArguments(args, context, env, out bool returnUndefined);
			if (returnUndefined)
			{
				return EvalProcessor.UNDEFINED;
			};

			object? resultObj;
			try
			{
				resultObj = this.m_methodInfo.Invoke(this.m_target, parameters);
			}
			catch (TargetInvocationException ti)
			{
				if (ti.InnerException is JsonataException)
				{
					ExceptionDispatchInfo.Capture(ti.InnerException).Throw();
				}
				else
				{
					throw new Exception($"Error evaluating function '{this.m_functionName}': {(ti.InnerException?.Message ?? "?")}", ti);
				}
				throw;
			}
			JToken result = this.ConvertFunctionResult(resultObj);
			return result;
		}

		private object?[] BindFunctionArguments(List<JToken> args, JToken? context, EvaluationEnvironment env, out bool returnUndefined)
		{
			try
			{
				return this.TryBindFunctionArguments(args, null, env, out returnUndefined);
			}
			catch (JsonataException)
			{
				//try binding with context if possible
				if (context != null && this.m_hasContextParameter)
				{
					return this.TryBindFunctionArguments(args, context, env, out returnUndefined);
				}
				else
				{
					throw;
				}
			};
		}


		private object?[] TryBindFunctionArguments(List<JToken> args, JToken? context, EvaluationEnvironment env, out bool returnUndefined)
		{
			returnUndefined = false;
			object?[] result = new object[this.m_parameters.Count];
			int sourceIndex = 0;
			for (int targetIndex = 0; targetIndex < this.m_parameters.Count; ++targetIndex)
			{
				ArgumentInfo argumentInfo = this.m_parameters[targetIndex];
				if (context != null && argumentInfo.allowContextAsValue)
                {
					//if we explicitly provide context, then hurry and use it!
					result[targetIndex] = this.ConvertFunctionArg(targetIndex, context, argumentInfo, out bool needReturnUndefined);
					if (needReturnUndefined)
					{
						returnUndefined = true;
					}
				}
				else if (argumentInfo.isEvaluationSupplement)
                {
					result[targetIndex] = env.GetEvaluationSupplement();
				}
				else if (sourceIndex >= args.Count)
				{
					if (argumentInfo.isOptional)
					{
						//use default value
						result[targetIndex] = argumentInfo.defaultValueForOptional;
					}
					else
					{
						throw new JsonataException("T0410", $"Function '{m_functionName}' requires {this.m_parameters.Count + (this.m_hasEnvParameter? -1 : 0)} arguments. Passed {args.Count} arguments");
					}
				}
				else if (argumentInfo.isVariableArgumentsArray)
				{
					//pack all remaining args to vararg.
					//TODO: Will not work if this is not last one in parameters list...
					JArray vararg = new JArray(args.Count);
					while (sourceIndex < args.Count)
                    {
						vararg.Add(args[sourceIndex]);
						++sourceIndex;
					}
					result[targetIndex] = vararg;
				}
				else
				{
					result[targetIndex] = this.ConvertFunctionArg(targetIndex, args[sourceIndex], argumentInfo, out bool needReturnUndefined);
					++sourceIndex;
					if (needReturnUndefined)
					{
						returnUndefined = true;
					}
				}
			};

			if (sourceIndex < args.Count)
			{
				throw new JsonataException("T0410", $"Function '{m_functionName}' requires {this.m_parameters.Count + (this.m_hasEnvParameter ? -1 : 0)} arguments. Passed {args.Count} arguments");
			};

			return result;
		}

		private object? ConvertFunctionArg(int parameterIndex, JToken argToken, ArgumentInfo argumentInfo, out bool returnUndefined)
		{
			//TODO: place all this reflection into FunctionTokenCsharp
			if (argToken.Type == JTokenType.Undefined)
			{
				if (argumentInfo.propagateUndefined)
				{
					returnUndefined = true;
					return EvalProcessor.UNDEFINED;
				}
				if (argumentInfo.isOptional)
				{
					//use default value instead of Undefined. This seem to be the case in JS
					returnUndefined = false;
					return argumentInfo.defaultValueForOptional;
				};
			};
			returnUndefined = false;

			if (argumentInfo.packSingleValueToSequence && argToken.Type != JTokenType.Array)
			{
				Sequence sequence = new Sequence();
				sequence.Add(argToken);
				argToken = sequence;
			};

			//TODO: add support for broadcasting Undefined
			if (argumentInfo.parameterType.IsAssignableFrom(argToken.GetType()))
			{
				return argToken;
			}
			else if (argumentInfo.parameterType == typeof(double))
			{
				switch (argToken.Type)
				{
				case JTokenType.Integer:
					return (double)(long)argToken;
				case JTokenType.Float:
					return (double)argToken;
				}
			}
			else if (argumentInfo.parameterType == typeof(float))
			{
				switch (argToken.Type)
				{
				case JTokenType.Integer:
					return (float)(long)argToken;
				case JTokenType.Float:
					return (float)(double)argToken;
				}
			}
			else if (argumentInfo.parameterType == typeof(int))
			{
				switch (argToken.Type)
				{
				case JTokenType.Integer:
					return (int)(long)argToken;
				case JTokenType.Float:
					return (int)(double)argToken; //jsonata seem to allow this
				}
			}
			else if (argumentInfo.parameterType == typeof(long))
			{
				switch (argToken.Type)
				{
				case JTokenType.Integer:
					return (long)argToken;
				case JTokenType.Float:
					return (long)(double)argToken; //jsonata seem to allow this
				}
			}
			else if (argumentInfo.parameterType == typeof(decimal))
			{
				switch (argToken.Type)
				{
				case JTokenType.Integer:
					return (decimal)(long)argToken;
				case JTokenType.Float:
					return (decimal)(double)argToken;
				}
			}
			else if (argumentInfo.parameterType == typeof(string))
			{
				switch (argToken.Type)
				{
				case JTokenType.String:
					return (string)argToken!;
				}
			}
			else if (argumentInfo.parameterType == typeof(bool))
			{
				switch (argToken.Type)
				{
				case JTokenType.Boolean:
					return (bool)argToken;
				}
			}
			throw new JsonataException("T0410", $"Argument {parameterIndex + 1} ('{argumentInfo.name}') of function {this.m_functionName} should be {argumentInfo.parameterType.Name} but incompatible value of type {argToken.Type} was specified");
		}

		private JToken ConvertFunctionResult(object? resultObj)
		{
			if (resultObj is JToken token)
			{
				return token;
			}
			else if (resultObj == null)
			{
				return JValue.CreateNull();
			}
			else if (resultObj is double resultDouble)
			{
				return ReturnDoubleResult(resultDouble);
			}
			else if (resultObj is float resultFloat)
			{
				return ReturnDoubleResult(resultFloat);
			}
			else if (resultObj is decimal resultDecimal)
			{
				return ReturnDecimalResult(resultDecimal);
			}
			else if (resultObj is int resultInt)
			{
				return new JValue(resultInt);
			}
			else if (resultObj is long resultLong)
			{
				return new JValue(resultLong);
			}
			else if (resultObj is string resultString)
			{
				return new JValue(resultString);
			}
			else if (resultObj is bool resultBool)
			{
				return new JValue(resultBool);
			}
			else
			{
				return JToken.FromObject(resultObj);
			}
		}

        public override JToken DeepClone()
        {
			return new FunctionTokenCsharp(this.m_functionName, this.m_methodInfo, this.m_target);
        }

        protected override void CleaParentNested()
        {
            //nothing to do for a function;
        }
    }
}

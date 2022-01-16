using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    internal static class EvalProcessor_Functions
    {
		internal static JToken CallCsharpFunction(string functionName, MethodInfo methodInfo, List<JToken> args, JToken? inputAsContext, Environment env)
		{
			ParameterInfo[] parameterList = methodInfo.GetParameters();

			object?[] parameters;
			try
			{
				parameters = BindFunctionArguments(functionName, parameterList, args, env, out bool returnUndefined);
				if (returnUndefined)
                {
					return EvalProcessor.UNDEFINED;
                }
			}
			catch (JsonataException)
            {
				//try binding with context if possible
				if (inputAsContext != null 
					&& args.Count < parameterList.Length 
					&& parameterList[0].IsDefined(typeof(AllowContextAsValueAttribute))
				)
				{
					List<JToken> newArgs = new List<JToken>(args.Count + 1);
					newArgs.Add(inputAsContext);
					newArgs.AddRange(args);
					parameters = BindFunctionArguments(functionName, parameterList, newArgs, env, out bool returnUndefined);
					if (returnUndefined)
					{
						return EvalProcessor.UNDEFINED;
					}
				}
				else
                {
					throw;
                }
			}
			object? resultObj;
			try
			{
				resultObj = methodInfo.Invoke(null, parameters);
			}
			catch (TargetInvocationException ti)
			{
				if (ti.InnerException is JsonataException)
				{
					ExceptionDispatchInfo.Capture(ti.InnerException).Throw();
				}
				else
				{
					throw new Exception($"Error evaluating function '{functionName}': {(ti.InnerException?.Message ?? "?")}", ti);
				}
				throw;
			}
			JToken result = ConvertFunctionResult(functionName, resultObj);
			return result;
		}

		private static object?[] BindFunctionArguments(string functionName, ParameterInfo[] parameterList, List<JToken> args, Environment env, out bool returnUndefined)
        {
			returnUndefined = false;
			object?[] parameters = new object[parameterList.Length];
			int i = 0;
			for (; i < parameterList.Length; ++i)
			{
				ParameterInfo parameterInfo = parameterList[i];
				if (i >= args.Count)
				{
					//TODO: place all this reflection into FunctionTokenCsharp
					OptionalArgumentAttribute? optional = parameterInfo.GetCustomAttribute<OptionalArgumentAttribute>();
					if (optional != null)
					{
						//use default value
						parameters[i] = optional.DefaultValue;
						continue;
					};
					if (parameterInfo.IsDefined(typeof(EvalEnvironmentArgumentAttribute), false))
					{
						if (parameterInfo.ParameterType != typeof(EvaluationEnvironment))
						{
							throw new Exception($"Declaration error for function '{functionName}': attribute [{nameof(EvalEnvironmentArgumentAttribute)}] can only be specified for arguments of type {nameof(EvaluationEnvironment)}");
						};
						parameters[i] = env.GetEvaluationEnvironment();
						continue;
					};
					throw new JsonataException("T0410", $"Function '{functionName}' requires {parameterList.Length} arguments. Passed {args.Count} arguments");
				}
				else if (parameterInfo.IsDefined(typeof(VariableNumberArgumentAsArrayAttribute), false))
                {
					if (parameterInfo.ParameterType != typeof(JArray))
					{
						throw new Exception($"Declaration error for function '{functionName}': attribute [{nameof(VariableNumberArgumentAsArrayAttribute)}] can only be specified for arguments of type {nameof(JArray)}");
					};

					//pack all remaining args to vararg.
					//TODO: Will not work if this is not last one in parameters list...
					JArray vararg = new JArray();
					for (int j = i; j < args.Count; ++j)
                    {
						vararg.Add(args[j]);
                    };
					parameters[i] = vararg;
					i = args.Count;
					break;
                }
				else
				{
					parameters[i] = ConvertFunctionArg(functionName, i, args[i], parameterInfo, out bool needReturnUndefined);
					if (needReturnUndefined)
					{
						returnUndefined = true;
					}
				}
			};

			if (i < args.Count)
            {
				throw new JsonataException("T0410", $"Function '{functionName}' requires {parameterList.Length} arguments. Passed {args.Count} arguments");
			};

			return parameters;
		}

		private static JToken ConvertFunctionResult(string functionName, object? resultObj)
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

		internal static JToken ReturnDoubleResult(double resultDouble)
		{
			if (Double.IsNaN(resultDouble) || Double.IsInfinity(resultDouble))
			{
				throw new JsonataException("D3030", "Jsonata does not support NaNs or Infinity values");
			};

			long resultLong = (long)resultDouble;
			if (resultLong == resultDouble)
			{
				return new JValue(resultLong);
			}
			else
			{
				return new JValue(resultDouble);
			}
		}

		internal static JToken ReturnDecimalResult(decimal resultDecimal)
		{
			long resultLong = (long)resultDecimal;
			if (resultLong == resultDecimal)
			{
				return new JValue(resultLong);
			}
			else
			{
				return new JValue(resultDecimal);
			}
		}

		private static object? ConvertFunctionArg(string functionName, int parameterIndex, JToken argToken, ParameterInfo parameterInfo, out bool returnUndefined)
		{
			//TODO: place all this reflection into FunctionTokenCsharp
			if (argToken.Type == JTokenType.Undefined)
			{
				if (parameterInfo.IsDefined(typeof(PropagateUndefinedAttribute), false))
				{
					returnUndefined = true;
					return EvalProcessor.UNDEFINED;
				}
				OptionalArgumentAttribute? optional = parameterInfo.GetCustomAttribute<OptionalArgumentAttribute>();
				if (optional != null)
				{
					//use default value instead of Undefined. This seem to be the case in JS
					returnUndefined = false;
					return optional.DefaultValue;
				};
			};
			returnUndefined = false;

			if (parameterInfo.IsDefined(typeof(PackSingleValueToSequenceAttribute), false)
				&& argToken.Type != JTokenType.Array
			)
            {
				Sequence sequence = new Sequence();
				sequence.Add(argToken);
				argToken = sequence;
            };

			//TODO: add support for broadcasting Undefined
			if (parameterInfo.ParameterType.IsAssignableFrom(argToken.GetType()))
			{
				return argToken;
			}
			else if (parameterInfo.ParameterType == typeof(double))
			{
				switch (argToken.Type)
				{
				case JTokenType.Integer:
					return (double)(long)argToken;
				case JTokenType.Float:
					return (double)argToken;
				}
			}
			else if (parameterInfo.ParameterType == typeof(float))
			{
				switch (argToken.Type)
				{
				case JTokenType.Integer:
					return (float)(long)argToken;
				case JTokenType.Float:
					return (float)(double)argToken;
				}
			}
			else if (parameterInfo.ParameterType == typeof(int))
			{
				switch (argToken.Type)
				{
				case JTokenType.Integer:
					return (int)(long)argToken;
				case JTokenType.Float:
					return (int)(double)argToken; //jsonata seem to allow this
				}
			}
			else if (parameterInfo.ParameterType == typeof(long))
			{
				switch (argToken.Type)
				{
				case JTokenType.Integer:
					return (long)argToken;
				case JTokenType.Float:
					return (long)(double)argToken; //jsonata seem to allow this
				}
			}
			else if (parameterInfo.ParameterType == typeof(decimal))
			{
				switch (argToken.Type)
				{
				case JTokenType.Integer:
					return (decimal)(long)argToken;
				case JTokenType.Float:
					return (decimal)(double)argToken;
				}
			}
			else if (parameterInfo.ParameterType == typeof(string))
            {
				switch (argToken.Type)
				{
				case JTokenType.String:
					return (string)argToken!;
				}
			}
			else if (parameterInfo.ParameterType == typeof(bool))
			{
				switch (argToken.Type)
				{
				case JTokenType.Boolean:
					return (bool)argToken;
				}
			}
			throw new JsonataException("T0410", $"Argument {parameterIndex + 1} ('{parameterInfo.Name}') of function {functionName} should be {parameterInfo.ParameterType.Name} bun incompatible value of type {argToken.Type} was specified");
		}
	}
}

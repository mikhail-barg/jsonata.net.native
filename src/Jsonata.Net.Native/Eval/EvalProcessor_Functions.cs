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
		internal static JToken CallCsharpFunction(FunctionTokenCsharp function, List<JToken> args, JToken? inputAsContext, Environment env)
		{
			object?[] parameters;
			try
			{
				parameters = BindFunctionArguments(function.functionName, function.parameters, args, env, out bool returnUndefined);
				if (returnUndefined)
                {
					return EvalProcessor.UNDEFINED;
                }
			}
			catch (JsonataException)
            {
				//try binding with context if possible
				if (inputAsContext != null 
					&& args.Count < function.parameters.Count
					&& function.parameters[0].allowContextAsValue
				)
				{
					List<JToken> newArgs = new List<JToken>(args.Count + 1);
					newArgs.Add(inputAsContext);
					newArgs.AddRange(args);
					parameters = BindFunctionArguments(function.functionName, function.parameters, newArgs, env, out bool returnUndefined);
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
				resultObj = function.methodInfo.Invoke(null, parameters);
			}
			catch (TargetInvocationException ti)
			{
				if (ti.InnerException is JsonataException)
				{
					ExceptionDispatchInfo.Capture(ti.InnerException).Throw();
				}
				else
				{
					throw new Exception($"Error evaluating function '{function.functionName}': {(ti.InnerException?.Message ?? "?")}", ti);
				}
				throw;
			}
			JToken result = ConvertFunctionResult(function.functionName, resultObj);
			return result;
		}

		private static object?[] BindFunctionArguments(string functionName, IReadOnlyList<FunctionTokenCsharp.ArgumentInfo> parameterList, List<JToken> args, Environment env, out bool returnUndefined)
        {
			returnUndefined = false;
			object?[] parameters = new object[parameterList.Count];
			int i = 0;
			for (; i < parameterList.Count; ++i)
			{
				FunctionTokenCsharp.ArgumentInfo argumentInfo = parameterList[i];
				if (i >= args.Count)
				{
					if (argumentInfo.isOptional)
					{
						//use default value
						parameters[i] = argumentInfo.defaultValueForOptional;
						continue;
					}
					else if (argumentInfo.isEvaluationEnvironment)
					{
						parameters[i] = env.GetEvaluationEnvironment();
						continue;
					}
					else
					{
						throw new JsonataException("T0410", $"Function '{functionName}' requires {parameterList.Count} arguments. Passed {args.Count} arguments");
					}
				}
				else if (argumentInfo.isVariableArgumentsArray)
                {
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
					parameters[i] = ConvertFunctionArg(functionName, i, args[i], argumentInfo, out bool needReturnUndefined);
					if (needReturnUndefined)
					{
						returnUndefined = true;
					}
				}
			};

			if (i < args.Count)
            {
				throw new JsonataException("T0410", $"Function '{functionName}' requires {parameterList.Count} arguments. Passed {args.Count} arguments");
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

		private static object? ConvertFunctionArg(string functionName, int parameterIndex, JToken argToken, FunctionTokenCsharp.ArgumentInfo argumentInfo, out bool returnUndefined)
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
			throw new JsonataException("T0410", $"Argument {parameterIndex + 1} ('{argumentInfo.name}') of function {functionName} should be {argumentInfo.parameterType.Name} but incompatible value of type {argToken.Type} was specified");
		}
	}
}

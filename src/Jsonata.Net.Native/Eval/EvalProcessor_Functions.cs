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
		internal static JToken CallFunction(string functionName, MethodInfo methodInfo, List<JToken> args)
		{
			ParameterInfo[] parameterList = methodInfo.GetParameters();
			if (parameterList.Length == 1
				&& parameterList[0].ParameterType == typeof(JArray)
				&& (args.Count > 1 
					|| (args.Count == 1 && !typeof(JArray).IsAssignableFrom(args[0].GetType()))
				)
			)
			{
				//convert args to array
				JArray array = new JArray();
				array.AddRange(args);
				args = new List<JToken>() { array };
			};

			if (args.Count > parameterList.Length)
			{
				throw new JsonataException("T0410", $"Function '{functionName}' requires {parameterList.Length} arguments. Passed {args.Count} arguments");
			};

			//parepare parameters
			object[] parameters = new object[parameterList.Length];
			for (int i = 0; i < parameterList.Length; ++i)
			{
				ParameterInfo parameterInfo = parameterList[i];
				if (i >= args.Count)
				{
					OptionalArgumentAttribute? optional = parameterInfo.GetCustomAttribute<OptionalArgumentAttribute>();
					if (optional == null)
					{
						throw new JsonataException("T0410", $"Function '{functionName}' requires {parameterList.Length} arguments. Passed {args.Count} arguments");
					}
					else
					{
						//use default value
						parameters[i] = optional.DefaultValue;
					}
				}
				else
				{
					parameters[i] = ConvertFunctionArg(functionName, i, args[i], parameterInfo, out bool returnUndefined);
					if (returnUndefined)
					{
						return EvalProcessor.UNDEFINED;
					}
				}
			};

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
			else
			{
				return JToken.FromObject(resultObj);
			}
		}

		private static JToken ReturnDoubleResult(double resultDouble)
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

		private static JToken ReturnDecimalResult(decimal resultDecimal)
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

		private static object ConvertFunctionArg(string functionName, int parameterIndex, JToken argToken, ParameterInfo parameterInfo, out bool returnUndefined)
		{
			if (argToken.Type == JTokenType.Undefined
				&& parameterInfo.GetCustomAttribute<PropagateUndefinedAttribute>() != null
			)
			{
				returnUndefined = true;
				return EvalProcessor.UNDEFINED;
			}
			else
			{
				returnUndefined = false;
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
				}
			}
			else if (parameterInfo.ParameterType == typeof(long))
			{
				switch (argToken.Type)
				{
				case JTokenType.Integer:
					return (long)argToken;
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
			throw new JsonataException("T0410", $"Argument {parameterIndex} ('{parameterInfo.Name}') of function {functionName} should be {parameterInfo.ParameterType.Name} bun incompatible value of type {argToken.Type} was specified");
		}
	}
}

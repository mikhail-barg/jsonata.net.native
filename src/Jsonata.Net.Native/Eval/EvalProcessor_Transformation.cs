using Jsonata.Net.Native.Parsing;
using Newtonsoft.Json;
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
    internal static class EvalProcessor_Transformation
	{
		internal static JToken CallTransformationFunction(FunctionTokenTransformation transformationFunction, List<JToken> args, JToken? inputAsContext)
		{
			if (args.Count != transformationFunction.ArgumentsCount)
            {
				throw new ApplicationException("Should not happen");
            }

			switch (args[0].Type)
            {
			case JTokenType.Undefined:
				return EvalProcessor.UNDEFINED;
			case JTokenType.Array:
			case JTokenType.Object:
				break;
			default:
				throw new JsonataException("T0410", $"Argument 1 of transform should be either Object or Array, got {args[0].Type}");
			}


			JToken arg = args[0].DeepClone();

			//TODO: huge problem with transformations — here matches (or its content) may be already detached from arg ((
			JToken matches = EvalProcessor.Eval(transformationFunction.pattern, arg, transformationFunction.environment);
			if (matches.Type != JTokenType.Undefined)
			{
				if (matches.Type != JTokenType.Array)
				{
					ProcessItem(matches, transformationFunction);
				}
				else
				{
					foreach (JToken child in matches.Children())
					{
						ProcessItem(child, transformationFunction);
					}
				}
			};
			return arg;
		}

        private static void ProcessItem(JToken item, FunctionTokenTransformation transformationFunction)
        {
			if (item.Type != JTokenType.Object)
			{
				//TODO:? 
				throw new JsonataException("????", $"Update can be applied only to objects. Got {item.Type} ({item.ToString(Formatting.None)})");
			};
			JObject srcObj = (JObject)item;

			//update
            {
                JToken update = EvalProcessor.Eval(transformationFunction.updates, item, transformationFunction.environment);
                if (update.Type != JTokenType.Undefined)
                {
                    if (update.Type != JTokenType.Object)
                    {
                        throw new JsonataException("T2011", $"The insert/update clause of the transform expression must evaluate to an object. Got {update.Type} ({update.ToString(Formatting.None)})");
                    };

                    srcObj.Merge(update);
                }
            }

			//delete
            if (transformationFunction.deletes != null)
			{
				JToken delete = EvalProcessor.Eval(transformationFunction.deletes, item, transformationFunction.environment);
				if (delete.Type != JTokenType.Undefined)
				{
					switch (delete.Type)
					{
					case JTokenType.String:
						Remove(srcObj, delete);
						break;
					case JTokenType.Array:
						foreach (JToken child in delete.Children())
						{
							Remove(srcObj, child);
						}
						break;
					default:
						throw new JsonataException("T2012", $"The delete clause of the transform expression must evaluate to a string or array of strings: {delete.Type} ({delete.ToString(Formatting.None)})");
					}
				}
			}
		}

		private static void Remove(JObject srcObj, JToken keyToRemove)
        {
			if (keyToRemove.Type != JTokenType.String)
            {
				throw new JsonataException("T2012", $"The delete clause of the transform expression must evaluate to a string or array of strings: {keyToRemove.Type} ({keyToRemove.ToString(Formatting.None)})");
			}
			srcObj.Remove((string)keyToRemove!);
		}
    }
}

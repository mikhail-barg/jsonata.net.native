using Jsonata.Net.Native.Json;
using Jsonata.Net.Native.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
	/**
	... ~> | ... | ... | (Transform)
	*/
	internal sealed class FunctionTokenTransformation : FunctionToken
	{
		internal readonly Node pattern;
		internal readonly Node updates;
		internal readonly Node? deletes;
		internal readonly EvaluationEnvironment environment;

		public FunctionTokenTransformation(Node pattern, Node updates, Node? deletes, EvaluationEnvironment environment)
			: base("transform", 1)
		{
			this.pattern = pattern;
			this.updates = updates;
			this.deletes = deletes;
			this.environment = environment;
		}

        /**
            The ~> operator is the operator for function chaining 
            and passes the value on the left hand side to the function on the right hand side as its first argument. 
        
            The expression on the right hand side must evaluate to a function, 
            hence the |...|...| syntax generates a function with one argument.         
         */

        internal override JToken Invoke(List<JToken> args, JToken? context, EvaluationEnvironment env)
        {
			if (args.Count != this.ArgumentsCount)
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

			JToken matches = EvalProcessor.Eval(this.pattern, arg, this.environment);
			if (matches.Type != JTokenType.Undefined)
			{
				if (matches.Type != JTokenType.Array)
				{
					this.ProcessItem(matches);
				}
				else
				{
					JArray matchesArray = (JArray)matches;
					foreach (JToken child in matchesArray.ChildrenTokens)
					{
						this.ProcessItem(child);
					}
				}
			};
			return arg;
		}
		private void ProcessItem(JToken item)
		{
			if (item.Type != JTokenType.Object)
			{
				//TODO:? 
				//throw new JsonataException("????", $"Update can be applied only to objects. Got {item.Type} ({item.ToStringFlat()})");
				return;
			};
			JObject srcObj = (JObject)item;

			//update
			{
				JToken update = EvalProcessor.Eval(this.updates, item, this.environment);
				if (update.Type != JTokenType.Undefined)
				{
					if (update.Type != JTokenType.Object)
					{
						throw new JsonataException("T2011", $"The insert/update clause of the transform expression must evaluate to an object. Got {update.Type} ({update.ToStringFlat()})");
					};

					srcObj.Merge((JObject)update);
				}
			}

			//delete
			if (this.deletes != null)
			{
				JToken delete = EvalProcessor.Eval(this.deletes, item, this.environment);
				if (delete.Type != JTokenType.Undefined)
				{
					switch (delete.Type)
					{
					case JTokenType.String:
						this.Remove(srcObj, delete);
						break;
					case JTokenType.Array:
						{
							JArray deleteArray = (JArray)delete;
							foreach (JToken child in deleteArray.ChildrenTokens)
							{
								this.Remove(srcObj, child);
							}
						}
						break;
					default:
						throw new JsonataException("T2012", $"The delete clause of the transform expression must evaluate to a string or array of strings: {delete.Type} ({delete.ToStringFlat()})");
					}
				}
			}
		}

		private void Remove(JObject srcObj, JToken keyToRemove)
		{
			if (keyToRemove.Type != JTokenType.String)
			{
				throw new JsonataException("T2012", $"The delete clause of the transform expression must evaluate to a string or array of strings: {keyToRemove.Type} ({keyToRemove.ToStringFlat()})");
			}
			srcObj.Remove((string)keyToRemove!);
		}

		public override JToken DeepClone()
		{
			return new FunctionTokenTransformation(this.pattern, this.updates, this.deletes, this.environment);
		}
	}
}

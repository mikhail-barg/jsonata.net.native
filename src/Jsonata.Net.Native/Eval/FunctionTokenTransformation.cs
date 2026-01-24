using Jsonata.Net.Native.Dom;
using Jsonata.Net.Native.Json;
using Jsonata.Net.Native.New;
using Jsonata.Net.Native.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    /**
	... ~> | ... | ... | (Transform)
	*/
    // see jsonata.js evaluateTransformExpression
    // this one is also registered as _jsonata_function
    internal sealed class FunctionTokenTransformation : FunctionToken
	{
		internal readonly New.Node pattern;
		internal readonly New.Node update;
		internal readonly New.Node? delete;
		internal readonly EvaluationEnvironment environment;

		public FunctionTokenTransformation(New.Node pattern, New.Node update, New.Node? delete, EvaluationEnvironment environment)
			: base("transform", 1, signature: new Signature("<(oa):o>"))
		{
            this.pattern = pattern;
			this.update = update;
			this.delete = delete;
			this.environment = environment;
		}


        internal override JToken Apply(JsThisArgument jsThis, List<JToken> args)
        {
            // transformer = async function (obj)

            JToken obj = args[0];

            // undefined inputs always return undefined
            if (obj.Type == JTokenType.Undefined)
			{
				return JsonataQ.UNDEFINED;
			}

			/*
			 var cloneFunction = environment.lookup('clone');
            if(!isFunction(cloneFunction)) {
                // throw type error
                throw {
                    code: "T2013",
                    stack: (new Error()).stack,
                    position: expr.position
                };
            }
            var result = await apply(cloneFunction, [obj], null, environment);
			*/
			JToken result = obj.DeepClone();

            JToken matches = JsonataQ.evaluate(this.pattern, result, this.environment);
            if (matches.Type != JTokenType.Undefined)
            {
                if (matches is not JArray matchesArray)
                {
                    matchesArray = new JArray();
					matchesArray.Add(matches);
                }
                foreach (JToken match in matchesArray.ChildrenTokens)
                {
					// javascript-specific
                    // if (match && (match.isPrototypeOf(result) || match instanceof Object.constructor)) 
					// {
					// 	throw { code: "D1010", stack: (new Error()).stack, position: expr.position };
					// }
					if (match is not JObject matchObject)
					{
						//throw new Exception("??");
						continue; 
					}

					// evaluate the update value for each match
					JToken update = JsonataQ.evaluate(this.update, match, this.environment);
					// update must be an object
					if (update.Type != JTokenType.Undefined)
					{
						if (update.Type != JTokenType.Object)
						{
							// throw type error
							throw new JException("T2011", this.update.position, update);
						}

                        // merge the update
                        foreach (KeyValuePair<string, JToken> prop in ((JObject)update).Properties)
						{
                            matchObject.Set(prop.Key, prop.Value);
						}
					}

					// delete, if specified, must be an array of strings (or single string)
					if (this.delete != null)
					{
						JToken deletions = JsonataQ.evaluate(this.delete, match, environment);
						if (deletions.Type != JTokenType.Undefined)
						{
							if (deletions is not JArray deletionsArray)
							{
								deletionsArray = new JArray();
                                deletionsArray.Add(deletions);
							}
							if (!Helpers.IsArrayOfStrings(deletionsArray))
							{
								// throw type error
								throw new JException("T2012", this.delete.position, deletions);
							}
							foreach (JToken del in deletionsArray.ChildrenTokens)
							{
								matchObject.Remove((string)(JValue)del);
							}
						}
					}
				}
			}
            return result;
        }

		public override JToken DeepClone()
		{
			return new FunctionTokenTransformation(this.pattern, this.update, this.delete, this.environment);
		}
    }
}

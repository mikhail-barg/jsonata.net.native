using Jsonata.Net.Native.Json;
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
    internal sealed class FunctionTokenLambda : FunctionToken
    {
        private readonly ProcessedSignature? signature;
        internal readonly List<string> paramNames;
        internal readonly Node body;
        internal readonly JToken context;
        internal readonly EvaluationEnvironment environment;


        private FunctionTokenLambda(ProcessedSignature? processedSignature, List<string> paramNames, Node body, JToken context, EvaluationEnvironment environment)
            : base("lambda", paramNames.Count)
        {
            this.signature = processedSignature;
            this.paramNames = paramNames;
            this.body = body;
            this.context = context;
            this.environment = environment;
        }


        internal FunctionTokenLambda(LambdaNode.Signature? signature, List<string> paramNames, Node body, JToken context, EvaluationEnvironment environment)
            : this(
                  signature != null? new ProcessedSignature(signature) : null, 
                  paramNames, body, context, environment
            )
        {
        }

        internal override JToken Invoke(List<JToken> args, JToken? context, EvaluationEnvironment env)
        {
            List<(string, JToken)> alignedArgs;
            if (this.signature != null)
            {
                List<JToken> processedArgs = this.signature.ValidateAndAlign(args, context);
                if (processedArgs.Count != this.ArgumentsCount)
                {
                    throw new Exception("Failed to align args via signature");
                }
                alignedArgs = new List<(string, JToken)>(processedArgs.Count);
                for (int i = 0; i < processedArgs.Count; ++i)
                {
                    alignedArgs.Add((this.paramNames[i], processedArgs[i]));
                }
            }
            else
            {
                alignedArgs = this.AlignArgs(args);
            }

            EvaluationEnvironment executionEnv = EvaluationEnvironment.CreateNestedEnvironment(this.environment);
            foreach ((string name, JToken value) in alignedArgs)
            {
                executionEnv.BindValue(name, value);
            };

            JToken result = EvalProcessor.Eval(this.body, this.context, executionEnv);
            return result;
        }

        private List<(string, JToken)> AlignArgs(List<JToken> args)
        {
            List<(string, JToken)> result = new List<(string, JToken)>(this.paramNames.Count);
            //for some reson jsonata does not care if function invocation args does not match expected number of args 
            // - in case when there's no signature specified
            // see for example lambdas.case010 test

            for (int i = 0; i < this.paramNames.Count; ++i)
            {
                JToken value;
                if (i >= args.Count)
                {
                    value = EvalProcessor.UNDEFINED;
                }
                else
                {
                    value = args[i];
                };
                result.Add((this.paramNames[i], value));
            }
            return result;
        }

        public override JToken DeepClone()
        {
            return new FunctionTokenLambda(this.signature, this.paramNames, this.body, context.DeepClone(), this.environment);
        }

        protected override void CleaParentNested()
        {
            //not sure if context parent should be cleared or not
            this.context.ClearParent();
        }

        private sealed class ProcessedSignature
        {
            private sealed class ProcessedParam
            {
                internal readonly LambdaNode.Param param;
                internal Regex? contextRegex;
                internal ProcessedSignature? subSignature;

                internal ProcessedParam(LambdaNode.Param param)
                {
                    this.param = param;
                }
            }

            private readonly List<ProcessedParam> m_params;
            private readonly Regex m_resultRegex;

            internal ProcessedSignature(LambdaNode.Signature signature)
            {
                (this.m_resultRegex, this.m_params) = BuildRegexForParamsList(signature.args);
            }

            //see signature.js validate
            internal List<JToken> ValidateAndAlign(List<JToken> args, JToken? inputAsContext)
            {
                StringBuilder suppliedSignatureBuilder = new StringBuilder();
                foreach (JToken arg in args)
                {
                    suppliedSignatureBuilder.Append(getSymbol(arg));
                }
                string suppliedSignature = suppliedSignatureBuilder.ToString();

                Match match = this.m_resultRegex.Match(suppliedSignature);
                if (!match.Success) 
                {
                    //see signature.js throwValidationError
                    //TODO: implement properly
                    throw new JsonataException("T0410", "Signature validation failed");
                }
                else
                {
                    List<JToken> validatedArgs = new List<JToken>();
                    int argIndex = 0;
                    for (int paramIndex = 0; paramIndex < this.m_params.Count; ++paramIndex)
                    {
                        ProcessedParam param = this.m_params[paramIndex];
                       string paramMatch = match.Groups[paramIndex + 1].Value;
                        if (paramMatch == "")
                        {
                            if (param.contextRegex != null)
                            {
                                if (inputAsContext == null)
                                {
                                    throw new JsonataException("T0411", "No context provided");
                                }
                                // substitute context value for missing arg
                                // first check that the context value is the right type
                                if (param.contextRegex.IsMatch(getSymbol(inputAsContext))) 
                                {
                                    validatedArgs.Add(inputAsContext);
                                }
                                else
                                {
                                    // context value not compatible with this argument
                                    throw new JsonataException("T0411", "Context value is incompatible with expected arg");
                                }
                            }
                            else
                            {
                                //not sure why it expected to accept an arg here - in original code
                                //validatedArgs.Add(arg);
                                //++argIndex;

                                validatedArgs.Add(JValue.CreateUndefined());
                            }
                        }
                        else
                        {
                            JToken arg = args[argIndex];
                            // may have matched multiple args (if the regex ends with a '+'
                            foreach (char single in paramMatch)
                            {
                                if (param.param.type == LambdaNode.ParamType.Array)
                                {
                                    if (single == 'm')
                                    {
                                        // missing (undefined)
                                        arg = JValue.CreateUndefined();
                                    }
                                    else
                                    {
                                        arg = args[argIndex];
                                        // is there type information on the contents of the array?
                                        if (param.subSignature != null)
                                        {
                                            if (single != 'a')
                                            {
                                                // the function expects an array. If it's not one, make it so
                                                JArray argReplacementArray = new JArray(1);
                                                argReplacementArray.Add(arg);
                                                arg = argReplacementArray;
                                            }

                                            JArray argArray = (JArray)arg;
                                            if (argArray.ChildrenTokens.Count > 0)
                                            {
                                                string itemType = getSymbol(argArray.ChildrenTokens[0]);
                                                //TODO: check itemType matches subSignature

                                                // make sure every item in the array is this type
                                                for (int remainingItemIndex = 1; remainingItemIndex < argArray.ChildrenTokens.Count; ++remainingItemIndex)
                                                {
                                                    if (itemType != getSymbol(argArray.ChildrenTokens[remainingItemIndex]))
                                                    {
                                                        throw new JsonataException("T0412", "array match error"); //TODO: proper text
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                validatedArgs.Add(arg);
                                ++argIndex;
                            } //foreach single
                        }
                    }
                    return validatedArgs;
                }
            }

            //see signature.js getSymbol()
            private static string getSymbol(JToken arg)
            {
                return arg.Type switch {
                    JTokenType.Function => "f",
                    JTokenType.String => "s",
                    JTokenType.Integer => "n",
                    JTokenType.Float => "n",
                    JTokenType.Boolean => "b",
                    JTokenType.Null => "l",
                    JTokenType.Array => "a",
                    JTokenType.Object => "o",
                    JTokenType.Undefined => "m",
                    _ => throw new Exception("Undexpected token type " + arg.Type)
                };
            }

            private static (Regex resultRegex, List<ProcessedParam> processedParams) BuildRegexForParamsList(List<LambdaNode.Param> args)
            {
                List<ProcessedParam> processedParams = new List<ProcessedParam>(args.Count);
                StringBuilder builder = new StringBuilder();
                builder.Append("^");
                foreach (LambdaNode.Param param in args)
                {
                    ProcessedParam processed = new ProcessedParam(param);

                    builder.Append("(");
                    if (param.option == LambdaNode.ParamOpt.Contextable)
                    {
                        StringBuilder subBuilder = new StringBuilder();
                        AppendParamTypeRegexSetString(subBuilder, param.type);
                        string paramRegex = subBuilder.ToString();
                        processed.contextRegex = new Regex("^" + paramRegex + "$", RegexOptions.Compiled);
                        builder.Append(paramRegex).Append("?");
                    }
                    else
                    {
                        AppendParamTypeRegexSetString(builder, param.type);
                        switch (param.option)
                        {
                        case LambdaNode.ParamOpt.None: break;
                        case LambdaNode.ParamOpt.Optional: builder.Append("?"); break;
                        case LambdaNode.ParamOpt.Variadic: builder.Append("+"); break;
                        default:
                            throw new Exception("Unexpected option " + param.option);
                        }
                    }
                    if (param.subSignature != null)
                    {
                        switch (param.type)
                        {
                        case LambdaNode.ParamType.Array:
                        case LambdaNode.ParamType.Func:
                            processed.subSignature = new ProcessedSignature(param.subSignature);
                            break;
                        default:
                            throw new JsonataException("S0401", "Type parameters can only be applied to functions and arrays");
                        }
                    }
                    builder.Append(")");

                    processedParams.Add(processed);
                }
                builder.Append("$");

                string resultRegexString = builder.ToString();
                Regex resultRegex = new Regex(resultRegexString, RegexOptions.Compiled);
                return (resultRegex, processedParams);
            }

            //see signature.js parseSignature()
            private static void AppendParamTypeRegexSetString(StringBuilder builder, LambdaNode.ParamType paramType)
            {
                if (paramType == LambdaNode.ParamType.Array)
                {
                    //  normally treat any value as singleton array
                    builder.Append("[asnblfom]");
                }
                else if (paramType == LambdaNode.ParamType.Func)
                {
                    builder.Append("f");    
                }
                else
                {
                    builder.Append("[");
                    foreach ((LambdaNode.ParamType subtype, string letter) in LambdaNode.s_paramTypePriorityLetters)
                    {
                        if (paramType.HasFlag(subtype))
                        {
                            builder.Append(letter);
                        }
                    }
                    builder.Append("m]");   //undefined is acceptable
                }
            }
        }
    }
}

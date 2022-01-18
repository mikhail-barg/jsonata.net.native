using Jsonata.Net.Native.Parsing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    internal abstract class FunctionToken: JConstructor
    {
        internal const JTokenType TYPE = JTokenType.Constructor;
        internal int ArgumentsCount { get; }

        protected FunctionToken(string jsonName, int argumentsCount)
            :base(jsonName)
        {
            this.ArgumentsCount = argumentsCount;
        }
        
    }

    internal sealed class FunctionTokenCsharp: FunctionToken
    {
        internal readonly MethodInfo methodInfo;
        internal IReadOnlyList<ArgumentInfo> parameters;
        internal readonly string functionName;

        internal FunctionTokenCsharp(string funcName, MethodInfo methodInfo)
            : base($"{methodInfo.DeclaringType?.Name}.{methodInfo.Name}", methodInfo.GetParameters().Length)
        {
            this.functionName = funcName;
            this.methodInfo = methodInfo;
            this.parameters = this.methodInfo.GetParameters()
                .Select(pi => new ArgumentInfo(funcName, pi))
                .ToList();
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
            internal readonly bool isEvaluationEnvironment;
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

                this.isEvaluationEnvironment = parameterInfo.IsDefined(typeof(EvalEnvironmentArgumentAttribute), false);
                if (this.isEvaluationEnvironment && parameterInfo.ParameterType != typeof(EvaluationEnvironment))
                {
                    throw new JsonataException("????", $"Declaration error for function '{functionName}': attribute [{nameof(EvalEnvironmentArgumentAttribute)}] can only be specified for arguments of type {nameof(EvaluationEnvironment)}");
                };

                this.isVariableArgumentsArray = parameterInfo.IsDefined(typeof(VariableNumberArgumentAsArrayAttribute), false);
                if (this.isVariableArgumentsArray && parameterInfo.ParameterType != typeof(JArray))
                {
                    throw new Exception($"Declaration error for function '{functionName}': attribute [{nameof(VariableNumberArgumentAsArrayAttribute)}] can only be specified for arguments of type {nameof(JArray)}");
                };
            }
        }
    }

    internal sealed class FunctionTokenLambda : FunctionToken
    {
        internal readonly LambdaNode.Signature? signature;
        internal readonly List<string> paramNames;
        internal readonly Node body;
        internal readonly JToken context;
        internal readonly Environment environment;


        internal FunctionTokenLambda(LambdaNode.Signature? signature, List<string> paramNames, Node body, JToken context, Environment environment)
            : base("lambda", paramNames.Count)
        {
            this.signature = signature;
            this.paramNames = paramNames;
            this.body = body;
            this.context = context;
            this.environment = environment;
        }
    }

    internal sealed class FunctionTokenPartial : FunctionToken
    {
        internal readonly FunctionToken func;
        internal readonly List<JToken?> argsOrPlaceholders;

        internal FunctionTokenPartial(FunctionToken func, List<JToken?> argsOrPlaceholders)
            : base(func.Name + "_partial", argsOrPlaceholders.Count(t => t == null))
        {
            this.func = func;
            this.argsOrPlaceholders = argsOrPlaceholders;
        }
    }

    /**
     ... ~> | ... | ... | (Transform)
     */
    internal sealed class FunctionTokenTransformation : FunctionToken
    {
        internal readonly Node pattern;
        internal readonly Node updates;
        internal readonly Node? deletes;
        internal readonly Environment environment;

        public FunctionTokenTransformation(Node pattern, Node updates, Node? deletes, Environment environment)
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
    }

    internal sealed class FunctionTokenRegex : FunctionToken
    {
        internal readonly Regex regex;

        public FunctionTokenRegex(Regex regex)
            : base("regex", 1)
        {
            this.regex = regex;
        }

        /**
            The ~> is the chain operator, and its use here implies that the result of /regex/ is a function. 
            We'll see below that this is in fact the case.         
         */
    }
}

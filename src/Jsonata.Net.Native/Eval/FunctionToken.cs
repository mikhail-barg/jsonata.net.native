using Jsonata.Net.Native.Json;
using Jsonata.Net.Native.New;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    //The object that is passed as "this" argument for js apply() call, mostly called `focus`
    // eg 
    //
    // var focus = {
    //      environment: env
    // };
    // var result = proc.apply(focus, args);
    //
    // or
    //
    // var focus = {
    //   environment: environment,
    //   input: input
    // };

    public sealed class JsThisArgument
    {
        public readonly EvaluationEnvironment Environment;
        public readonly JToken Input;

        public JsThisArgument(EvaluationEnvironment environment, JToken input)
        {
            this.Environment = environment;
            this.Input = input;
        }
    }

    public abstract class FunctionToken: JToken
    {
        internal readonly string Name;
        internal readonly Signature? Signature;
        internal int ArgumentsCount { get; }
        //used in High-order functions to implement filters properly
        public int RequiredArgsCount { get; protected set; }    //in JS function.length returns number of params without default value

        protected FunctionToken(string jsonName, int argumentsCount, Signature? signature)
            : base(JTokenType.Function)
        {
            this.Name = jsonName;
            this.ArgumentsCount = argumentsCount;
            this.RequiredArgsCount = argumentsCount;
            this.Signature = signature;
        }

        //implementation of js apply() call
        internal abstract JToken Apply(JsThisArgument jsThis, List<JToken> args);

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

        internal override void ToIndentedStringImpl(StringBuilder builder, int indent, SerializationSettings options)
        {
            //throw new NotImplementedException("No supported for functions!");
            //builder.Append('$').Append(this.Name);
            builder.Append('"').Append('"');
        }

        internal override void ToStringFlatImpl(StringBuilder builder, SerializationSettings options)
        {
            //throw new NotImplementedException("No supported for functions!");
            //builder.Append('$').Append(this.Name);
            builder.Append('"').Append('"');
        }

        public override bool DeepEquals(JToken other)
        {
            throw new NotSupportedException("Not supported for functions");
        }
    }
}

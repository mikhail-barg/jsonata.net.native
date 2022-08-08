using Jsonata.Net.Native.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Eval
{
    internal abstract class FunctionToken: JToken
    {
        internal readonly string Name;
        internal int ArgumentsCount { get; }
        //used in High-order functions to implement filters properly
        public int RequiredArgsCount { get; protected set; }

        protected FunctionToken(string jsonName, int argumentsCount)
            : base(JTokenType.Function)
        {
            this.Name = jsonName;
            this.ArgumentsCount = argumentsCount;
            this.RequiredArgsCount = argumentsCount;
        }

        internal abstract JToken Invoke(List<JToken> args, JToken? context, EvaluationEnvironment env);


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

        internal override void ToIndentedStringImpl(StringBuilder builder, int indent)
        {
            //throw new NotImplementedException("No supported for functions!");
            //builder.Append('$').Append(this.Name);
            builder.Append('"').Append('"');
        }

        internal override void ToStringFlatImpl(StringBuilder builder)
        {
            //throw new NotImplementedException("No supported for functions!");
            //builder.Append('$').Append(this.Name);
            builder.Append('"').Append('"');
        }

        public override bool DeepEquals(JToken other)
        {
            throw new NotImplementedException("Not supported for functions");
        }
    }
}

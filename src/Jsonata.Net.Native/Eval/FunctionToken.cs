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

        internal abstract JToken Invoke(List<JToken> args, JToken? context, Environment env);


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
    }
}

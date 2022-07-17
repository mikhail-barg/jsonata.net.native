using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    internal class JValue: JToken
    {
        internal static JValue CreateUndefined()
        {
            return new JValue(JTokenType.Undefined, null);
        }

        internal static JToken CreateNull()
        {
            return new JValue(JTokenType.Null, null);
        }

        internal readonly object? Value;


        private JValue(JTokenType type, object? value)
            : base(type)
        {
            this.Value = value;
        }

        internal JValue(double value) : this(JTokenType.Float, DoubleToDecimal(value)) {}
        internal JValue(decimal value) : this(JTokenType.Float, value) { }
        internal JValue(long value) : this(JTokenType.Integer, value) { }
        internal JValue(int value) : this(JTokenType.Integer, value) { }
        internal JValue(string value) : this(JTokenType.String, value) { }
        internal JValue(char value) : this(JTokenType.String, value.ToString()) { }
        internal JValue(bool value) : this(JTokenType.Boolean, value) { }

        private static decimal DoubleToDecimal(double value)
        {
            if (Double.IsInfinity(value) || Double.IsNaN(value))
            {
                throw new JsonataException("S0102", "Number out of range: " + value);
            }
            try
            {
                return (decimal)value;
            }
            catch (Exception ex)
            {
                throw new JsonataException("S0102", $"Number out of range: {value} ({ex.Message})");
            }
        }

        private void ToString(StringBuilder builder)
        {
            switch (this.Type)
            {
            case JTokenType.Null:
                builder.Append("null");
                break;
            case JTokenType.Undefined:
                builder.Append("undefined");
                break;
            case JTokenType.Float:
                builder.Append(((decimal)this).ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
                break;
            case JTokenType.Integer:
                builder.Append(((long)this).ToString(CultureInfo.InvariantCulture));
                break;
            case JTokenType.String:
                builder.Append('"').Append((string)this).Append('"');
                break;
            case JTokenType.Boolean:
                builder.Append((bool)this? "true" : "false");
                break;
            default:
                throw new Exception("Unexpected type " + this.Type);
            }
        }

        internal override void ToIndentedStringImpl(StringBuilder builder, int indent)
        {
            this.ToString(builder);
        }

        internal override void ToStringFlatImpl(StringBuilder builder)
        {
            this.ToString(builder);
        }

        internal override Newtonsoft.Json.Linq.JToken ToNewtonsoft()
        {
            switch (this.Type)
            {
            case JTokenType.Null:
                return Newtonsoft.Json.Linq.JValue.CreateNull();
            case JTokenType.Undefined:
                return Newtonsoft.Json.Linq.JValue.CreateUndefined();
            case JTokenType.Float:
                return new Newtonsoft.Json.Linq.JValue((decimal)this);
            case JTokenType.Integer:
                return new Newtonsoft.Json.Linq.JValue((long)this);
            case JTokenType.String:
                return Newtonsoft.Json.Linq.JValue.CreateString((string)this);
            case JTokenType.Boolean:
                return new Newtonsoft.Json.Linq.JValue((bool)this);
            default:
                throw new Exception("Unexpected type " + this.Type);
            }
        }

        internal override JToken DeepClone()
        {
            return new JValue(this.Type, this.Value);
        }

        internal override bool DeepEquals(JToken other)
        {
            if (this.Type != other.Type)
            {
                return false;
            }
            JValue otherValue = (JValue)other;
            if (this.Value == otherValue.Value)
            {
                return true;
            }


            switch (this.Type)
            {

            case JTokenType.Float:
                {
                    decimal thisV = (decimal)this;
                    decimal otherV = (decimal)otherValue;
                    return Decimal.Compare(thisV, otherV) == 0;
                }
            case JTokenType.Integer:
                {
                    long thisV = (long)this;
                    long otherV = (long)otherValue;
                    return thisV == otherV;
                }
            case JTokenType.String:
                {
                    string thisV = (string)this;
                    string otherV = (string)other;
                    return String.CompareOrdinal(thisV, otherV) == 0;
                }
            case JTokenType.Boolean:
                {
                    bool thisV = (bool)this;
                    bool otherV = (bool)otherValue;
                    return thisV == otherV;
                }
            case JTokenType.Null:
            case JTokenType.Undefined:
            default:
                throw new Exception("Unexpected type " + this.Type);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    public class JValue: JToken
    {
        public static JValue CreateUndefined()
        {
            return new JValue(JTokenType.Undefined, null);
        }

        public static JToken CreateNull()
        {
            return new JValue(JTokenType.Null, null);
        }

        internal readonly object? Value;


        private JValue(JTokenType type, object? value)
            : base(type)
        {
            this.Value = value;
        }

        public JValue(double value) : this(JTokenType.Float, DoubleToDecimal(value)) {}
        public JValue(decimal value) : this(JTokenType.Float, value) { }
        public JValue(long value) : this(JTokenType.Integer, value) { }
        public JValue(int value) : this(JTokenType.Integer, value) { }
        public JValue(string value) : this(JTokenType.String, value) { }
        public JValue(char value) : this(JTokenType.String, value.ToString()) { }
        public JValue(bool value) : this(JTokenType.Boolean, value) { }

        private static object DoubleToDecimal(double value)
        {
            if (Double.IsInfinity(value) || Double.IsNaN(value))
            {
                throw new JsonataException("S0102", "Number out of range: " + value);
            }
            try
            {
                return (decimal)value;
            }
            /*
            catch (Exception ex)
            {
                throw new JsonataException("S0102", $"Number out of range: {value} ({ex.Message})");
            }
            */
            catch (Exception)
            {
                return value;
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
                if (this.Value is decimal decimalValue)
                {
                    builder.Append(decimalValue.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
                }
                else
                {
                    builder.Append(((double)this).ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
                }
                break;
            case JTokenType.Integer:
                builder.Append(((long)this).ToString(CultureInfo.InvariantCulture));
                break;
            case JTokenType.String:
                builder.Append('"');
                JToken.EscapeString((string)this, builder);
                builder.Append('"');
                break;
            case JTokenType.Boolean:
                builder.Append((bool)this? "true" : "false");
                break;
            default:
                throw new Exception("Unexpected type " + this.Type);
            }
        }

        protected override void ClearParentNested()
        {
            //nothing to do for a value;
        }

        internal override void ToIndentedStringImpl(StringBuilder builder, int indent, SerializationSettings options)
        {
            this.ToString(builder);
        }

        internal override void ToStringFlatImpl(StringBuilder builder, SerializationSettings options)
        {
            this.ToString(builder);
        }

        public override JToken DeepClone()
        {
            return new JValue(this.Type, this.Value);
        }

        //see Newtonsoft.Json.Utilities.MathUtils.ApproxEquals
        private static bool ApproxEquals(double d1, double d2)
        {
            const double epsilon = 2.2204460492503131E-16;

            if (d1 == d2)
            {
                return true;
            }

            double tolerance = ((Math.Abs(d1) + Math.Abs(d2)) + 10.0) * epsilon;
            double difference = d1 - d2;

            return (-tolerance < difference && tolerance > difference);
        }

        public override bool DeepEquals(JToken other)
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
                    try
                    {
                        decimal thisV = (decimal)this;
                        decimal otherV = (decimal)otherValue;
                        return Decimal.Compare(thisV, otherV) == 0;
                    }
                    catch (System.OverflowException)
                    {
                        double thisV = (double)this;
                        double otherV = (double)otherValue;
                        return ApproxEquals(thisV, otherV);
                    }
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

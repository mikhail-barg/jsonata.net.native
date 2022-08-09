using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    internal sealed class JsonParser
    {
        private readonly TextReader m_reader;
        internal int CurrentPosition = 0;

        internal JsonParser(TextReader reader)
        {
            this.m_reader = reader;
        }

        internal JToken Parse()
        {
            this.SkipWhitespace();
            JToken result = this.ParseAnyToken();
            this.SkipWhitespace();
            if (this.m_reader.Peek() >= 0)
            {
                throw new JsonParseException(this, "Unexpected continuation of data");
            }
            return result;
        }

        private JToken ParseAnyToken()
        {
            char c = this.PeekChar();
            switch (c)
            {
            case '{':
                return this.ParseObject();
            case '[':
                return this.ParseArray();
            case 't':
                this.ParseLiteral("true");
                return new JValue(true);
            case 'f':
                this.ParseLiteral("false");
                return new JValue(false);
            case 'n':
                this.ParseLiteral("null");
                return JValue.CreateNull();
            case 'u':
                this.ParseLiteral("undefined"); //not too standard-compilant
                return JValue.CreateUndefined();
            case '\'':
            case '"':
                return new JValue(this.ParseStringValue());
            case '-':
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                return this.ParseNumberToken();
            default:
                throw new JsonParseException(this, $"Expected a token start, got unexpected characer '{c}'");
            }
        }

        private JToken ParseNumberToken()
        {
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                int ci = this.m_reader.Peek();
                if (ci < 0)
                {
                    break;
                }
                char c = (char)ci;

                switch (c)
                {
                case '-':
                case '+':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '.':
                case 'e':
                case 'E':
                    sb.Append(c);
                    this.ReadChar();
                    break;
                default:
                    goto loopEnd;   //yes, using goto, and not even ashamed of it!
                }
            }
        loopEnd:
            string s = sb.ToString();
            if (!s.Contains(".") && !s.Contains('e') && !s.Contains('E'))
            {
                NumberStyles intStyle = NumberStyles.AllowLeadingSign;

                if (Int32.TryParse(s, intStyle, CultureInfo.InvariantCulture, out int intValue))
                {
                    return new JValue(intValue);
                }
                else if (Int64.TryParse(s, intStyle, CultureInfo.InvariantCulture, out long longValue))
                {
                    return new JValue(longValue);
                }
                else if (Decimal.TryParse(s, intStyle, CultureInfo.InvariantCulture, out decimal decValue))
                {
                    return new JValue(decValue);
                }
                else
                {
                    throw new JsonParseException(this, $"Failed to parse an integer: '{s}'");
                }
            }
            else
            {
                NumberStyles numStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
                if (Decimal.TryParse(s, numStyle, CultureInfo.InvariantCulture, out decimal decimalResult))
                {
                    return new JValue(decimalResult);
                }
                else if (Double.TryParse(s, numStyle, CultureInfo.InvariantCulture, out double doubleResult))
                {
                    return new JValue(doubleResult);
                }
                else
                {
                    throw new JsonParseException(this, $"Failed to parse a number: '{s}'");
                }
            }
        }

        private void ParseLiteral(string literal)
        {
            for (int i = 0; i < literal.Length; ++i)
            {
                this.ConsumeChar(literal[i]);
            }
        }

        private JToken ParseArray()
        {
            JArray result = new JArray();
            this.ConsumeChar('[');
            this.SkipWhitespace();
            bool hadComma = false;
            while (this.PeekChar() != ']')
            {
                if (result.Count > 0 && !hadComma)
                {
                    throw new JsonParseException(this, "Missing comma in array");
                }

                JToken value = this.ParseAnyToken();
                this.SkipWhitespace();
                if (this.PeekChar() == ',')
                {
                    this.ConsumeChar(',');  //allow trailing comma
                    this.SkipWhitespace();
                    hadComma = true;
                }
                else
                {
                    hadComma = false;
                }

                result.Add(value);
            }
            this.ConsumeChar(']');
            return result;
        }

        private JObject ParseObject()
        {
            JObject result = new JObject();
            this.ConsumeChar('{');
            this.SkipWhitespace();
            bool hadComma = false;
            while (this.PeekChar() != '}')
            {
                if (result.Count > 0 && !hadComma)
                {
                    throw new JsonParseException(this, "Missing comma in object");
                }

                string key = this.ParseStringValue();
                this.SkipWhitespace();
                this.ConsumeChar(':');
                this.SkipWhitespace();
                JToken value = this.ParseAnyToken();
                this.SkipWhitespace();
                if (this.PeekChar() == ',')
                {
                    this.ConsumeChar(',');  //allow trailing comma
                    this.SkipWhitespace();
                    hadComma = true;
                }
                else
                {
                    hadComma = false;
                }

                //result.Add(key, value);
                result.Set(key, value); //allowing duplicates
            }
            this.ConsumeChar('}');
            return result;
        }

        private string ParseStringValue()
        {
            char quoteChar = this.PeekChar();
            if (quoteChar != '\'' && quoteChar != '"')
            {
                throw new JsonParseException(this, $"Expected string start, but got '{quoteChar}'");
            }

            StringBuilder builder = new StringBuilder();
            this.ConsumeChar(quoteChar);
            while (this.PeekChar() != quoteChar)
            {
                builder.Append(this.ReadChar());
            }
            this.ConsumeChar(quoteChar);

            string result = builder.ToString();
            try
            {
                result = Regex.Unescape(result);
            }
            catch (Exception ex)
            {
                throw new JsonParseException(this, $"Failed to unescape sequence '{result}': {ex.Message}");
            }
            return result;
        }

        private void SkipWhitespace()
        {
            while (true)
            {
                int ci = this.m_reader.Peek();
                if (ci < 0)
                {
                    break;
                }
                char c = (char)ci;
                if (!Char.IsWhiteSpace(c))
                {
                    break;
                }
                ConsumeChar(c);
            }
        }

        private char PeekChar()
        {
            int ci = this.m_reader.Peek();
            if (ci < 0)
            {
                throw new JsonParseException(this, "Expected some char, but got end of stream");
            }
            return (char)ci;
        }

        private void ConsumeChar(char expectedChar)
        {
            int ci = this.m_reader.Read();
            if (ci < 0)
            {
                throw new JsonParseException(this, $"Expected '{expectedChar}' but got end of stream");
            }
            char c = (char)ci;
            if (c != expectedChar)
            {
                throw new JsonParseException(this, $"Expected '{expectedChar}' but got '{c}'");
            }
            ++this.CurrentPosition;
        }

        private char ReadChar()
        {
            int ci = this.m_reader.Read();
            if (ci < 0)
            {
                throw new JsonParseException(this, $"Expected some char but got end of stream");
            }
            char c = (char)ci;
            ++this.CurrentPosition;
            return c;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Json
{
    internal sealed class JsonParserAsync
    {
        private readonly TextReaderWrapper m_reader;
        private readonly ParseSettings m_settings;
        internal int CurrentPosition = 0;

        internal JsonParserAsync(TextReader reader, ParseSettings settings)
        {
            this.m_settings = settings;
            this.m_reader = new TextReaderWrapper(reader);
        }

        internal async Task<JToken> ParseAsync(CancellationToken ct)
        {
            await this.SkipWhitespaceAsync(ct);
            JToken result = await this.ParseAnyTokenAsync(ct);
            await this.SkipWhitespaceAsync(ct);
            if (await this.m_reader.PeekAsync(ct) >= 0)
            {
                throw new JsonParseException(this, "Unexpected continuation of data");
            }
            return result;
        }

        private async Task<JToken> ParseAnyTokenAsync(CancellationToken ct)
        {
            char c = await this.PeekCharAsync(ct);
            switch (c)
            {
            case '{':
                return await this.ParseObjectAsync(ct);
            case '[':
                return await this.ParseArrayAsync(ct);
            case 't':
                await this.ParseLiteralAsync("true", ct);
                return new JValue(true);
            case 'f':
                await this.ParseLiteralAsync("false", ct);
                return new JValue(false);
            case 'n':
                await this.ParseLiteralAsync("null", ct);
                return JValue.CreateNull();
            case 'u':
                await this.ParseLiteralAsync("undefined", ct); //not too standard-compilant
                return JValue.CreateUndefined();
            case '\'':
            case '"':
                return new JValue(await this.ParseStringValueAsync(ct));
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
                return await this.ParseNumberTokenAsync(ct);
            default:
                throw new JsonParseException(this, $"Expected a token start, got unexpected characer '{c}'");
            }
        }

        private async Task<JToken> ParseNumberTokenAsync(CancellationToken ct)
        {
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                int ci = await this.m_reader.PeekAsync(ct);
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
                    await this.ReadCharAsync(ct);
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

        private async Task ParseLiteralAsync(string literal, CancellationToken ct)
        {
            for (int i = 0; i < literal.Length; ++i)
            {
                await this.ConsumeCharAsync(literal[i], ct);
            }
        }

        private async Task<JToken> ParseArrayAsync(CancellationToken ct)
        {
            JArray result = new JArray();
            await this.ConsumeCharAsync('[', ct);
            await this.SkipWhitespaceAsync(ct);
            bool hadComma = false;
            while (await this.PeekCharAsync(ct) != ']')
            {
                if (result.Count > 0 && !hadComma)
                {
                    throw new JsonParseException(this, "Missing comma in array");
                }

                JToken value = await this.ParseAnyTokenAsync(ct);
                await this.SkipWhitespaceAsync(ct);
                if (await this.PeekCharAsync(ct) == ',')
                {
                    await this.ConsumeCharAsync(',', ct);  //allow trailing comma
                    await this.SkipWhitespaceAsync(ct);
                    hadComma = true;
                }
                else
                {
                    hadComma = false;
                }

                result.Add(value);
            }

            if (hadComma && !this.m_settings.AllowTrailingComma)
            {
                throw new JsonParseException(this, "Trailing comma in an array");
            }

            await this.ConsumeCharAsync(']', ct);
            return result;
        }

        private async Task<JObject> ParseObjectAsync(CancellationToken ct)
        {
            JObject result = new JObject();
            await this.ConsumeCharAsync('{', ct);
            await this.SkipWhitespaceAsync(ct);
            bool hadComma = false;
            while (await this.PeekCharAsync(ct) != '}')
            {
                if (result.Count > 0 && !hadComma)
                {
                    throw new JsonParseException(this, "Missing comma in object");
                }

                string key = await this.ParseStringValueAsync(ct);
                await this.SkipWhitespaceAsync(ct);
                await this.ConsumeCharAsync(':', ct);
                await this.SkipWhitespaceAsync(ct);
                JToken value = await this.ParseAnyTokenAsync(ct);
                await this.SkipWhitespaceAsync(ct);
                if (await this.PeekCharAsync(ct) == ',')
                {
                    await this.ConsumeCharAsync(',', ct);  //allow trailing comma
                    await this.SkipWhitespaceAsync(ct);
                    hadComma = true;
                }
                else
                {
                    hadComma = false;
                }

                //result.Add(key, value);
                result.Set(key, value); //allowing duplicates
            }

            if (hadComma && !this.m_settings.AllowTrailingComma)
            {
                throw new JsonParseException(this, "Trailing comma in an object");
            }

            await this.ConsumeCharAsync('}', ct);
            return result;
        }

        private async Task<string> ParseStringValueAsync(CancellationToken ct)
        {
            char quoteChar = await this.PeekCharAsync(ct);
            switch (quoteChar)
            {
            case '"':
                break; //regular
            case '\'':
                if (!this.m_settings.AllowSinglequoteStrings)
                {
                    throw new JsonParseException(this, "Single-quote strings are disabled in settings");
                }
                break;
            default:
                throw new JsonParseException(this, $"Expected string start, but got '{quoteChar}'");
            }

            StringBuilder builder = new StringBuilder();
            await this.ConsumeCharAsync(quoteChar, ct);
            bool prevCharIsBackslash = false;
            char currentChar = await this.PeekCharAsync(ct);
            while (true)
            {
                if (currentChar == quoteChar && !prevCharIsBackslash)
                {
                    break;
                }

                if (currentChar >= (char)0x00 && currentChar <= (char)0x1F
                    && !this.m_settings.AllowUnescapedControlChars
                )
                {
                    throw new JsonParseException(this, $"Settings forbid using unescaped control chars (0x{(int)currentChar:X})");
                }

                builder.Append(await this.ReadCharAsync(ct));

                if (currentChar == '\\')
                {
                    prevCharIsBackslash = !prevCharIsBackslash; //support for escaping backslash :'\\'
                }
                else
                {
                    prevCharIsBackslash = false;
                }

                currentChar = await this.PeekCharAsync(ct);
            }
            await this.ConsumeCharAsync(quoteChar, ct);

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

        private async Task SkipWhitespaceAsync(CancellationToken ct)
        {
            while (true)
            {
                int ci = await this.m_reader.PeekAsync(ct);
                if (ci < 0)
                {
                    break;
                }
                char c = (char)ci;
                if (!this.m_settings.IsWhiteSpace(c))
                {
                    break;
                }
                await this.ConsumeCharAsync(c, ct);
            }
        }

        private async Task<char> PeekCharAsync(CancellationToken ct)
        {
            int ci = await this.m_reader.PeekAsync(ct);
            if (ci < 0)
            {
                throw new JsonParseException(this, "Expected some char, but got end of stream");
            }
            return (char)ci;
        }

        private async Task ConsumeCharAsync(char expectedChar, CancellationToken ct)
        {
            int ci = await this.m_reader.ReadAsync(ct);
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

        private async Task<char> ReadCharAsync(CancellationToken ct)
        {
            int ci = await this.m_reader.ReadAsync(ct);
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

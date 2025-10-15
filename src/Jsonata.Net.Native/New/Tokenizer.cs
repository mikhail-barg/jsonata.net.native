using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization;

namespace Jsonata.Net.Native.New
{
    public sealed class Token
    {
        public SymbolType type;
        public object? value;
        public int position;

        public Token(SymbolType type, object? value, int position) 
        { 
            this.type = type;
            this.value = value;
            this.position = position;
        }
    }

    public sealed class Tokenizer  // = function (path) {
    {
        internal static Dictionary<string, int> operators = new() 
        {
            ["."] = 75,
            ["["] = 80,
            ["]"] = 0,
            ["{"] = 70,
            ["}"] = 0,
            ["("] = 80,
            [")"] = 0,
            [","] = 0,
            ["@"] = 80,
            ["#"] = 80,
            [";"] = 80,
            [":"] = 80,
            ["?"] = 20,
            ["+"] = 50,
            ["-"] = 50,
            ["*"] = 60,
            ["/"] = 60,
            ["%"] = 60,
            ["|"] = 20,
            ["="] = 40,
            ["<"] = 40,
            [">"] = 40,
            ["^"] = 40,
            ["**"] = 60,
            [".."] = 20,
            [":="] = 10,
            ["!="] = 40,
            ["<="] = 40,
            [">="] = 40,
            ["~>"] = 40,
            ["?:"] = 40,
            ["??"] = 40,
            ["and"] = 30,
            ["or"] = 25,
            ["in"] = 40,
            ["&"] = 50,
            ["!"] = 0,
            ["~"] = 0
        };

        static Dictionary<string, string> escapes = new()
        {
            // JSON string escape sequences - see json.org
            ["\""] = "\"",
            ["\\"] = "\\",
            ["/"] = "/",
            ["b"] = "\b",
            ["f"] = "\f",
            ["n"] = "\n",
            ["r"] = "\r",
            ["t"] = "\t",
        };

        // Tokenizer (lexer) - invoked by the parser to return one token at a time
        String path;
        int position = 0;
        int length; // = path.length;

        internal Tokenizer(string path) 
        {
            this.path = path;
            this.length = path.Length;
        }

        Token create(SymbolType type, object? value) 
        {
            Token t = new Token(type, value, this.position);
            return t;
        }

        private static Regex s_numregex = new Regex(@"^-?(0|([1-9][0-9]*))(\.[0-9]+)?([Ee][-+]?[0-9]+)?", RegexOptions.Compiled);
        int depth;

        bool isClosingSlash(int position) 
        {
            if (this.path[position] == '/' && depth == 0) 
            {
                int backslashCount = 0;
                while (path[position - (backslashCount + 1)] == '\\') 
                {
                    ++backslashCount;
                }
                if (backslashCount % 2 == 0) 
                {
                    return true;
                }
            }
            return false;
        }

        Regex scanRegex() 
        {
            // the prefix '/' will have been previously scanned. Find the end of the regex.
            // search for closing '/' ignoring any that are escaped, or within brackets
            int start = this.position;
            //int depth = 0;
            String pattern;
            String flags;

            while (this.position < this.length) 
            {
                char currentChar = this.path[this.position];
                if (isClosingSlash(this.position)) 
                {
                    // end of regex found
                    pattern = path.Substring(start, this.position - start);
                    if (pattern == "") 
                    {
                        throw new JException("S0301", this.position);
                    }
                    ++this.position;
                    currentChar = this.path[this.position];
                    // flags
                    start = this.position;
                    while (currentChar == 'i' || currentChar == 'm') 
                    {
                        ++this.position;
                        if (this.position < this.length) 
                        {
                            currentChar = this.path[this.position];
                        } 
                        else 
                        {
                            currentChar = (char)0;
                        }
                    }
                    flags = this.path.Substring(start, this.position - start) + 'g';
                    // Convert flags to Java Pattern flags
                    RegexOptions _flags = RegexOptions.Compiled;
                    if (flags.Contains('i'))
                    {
                        _flags |= RegexOptions.IgnoreCase;
                    }
                    if (flags.Contains('m'))
                    {
                        _flags |= RegexOptions.Multiline;
                    }
                    return new Regex(pattern, _flags); // Pattern.CASE_INSENSITIVE | Pattern.MULTILINE | Pattern.DOTALL);
                }
                if ((currentChar == '(' || currentChar == '[' || currentChar == '{') && this.path[this.position - 1] != '\\') 
                {
                    ++this.depth;
                }
                if ((currentChar == ')' || currentChar == ']' || currentChar == '}') && this.path[this.position - 1] != '\\') 
                {
                    --this.depth;
                }
                ++this.position;
            }
            throw new JException("S0302", position);
        }

        internal Token? next(bool prefix) 
        {
            if (this.position >= this.length)
            {
                return null;
            }
            char currentChar = this.path[this.position];
            // skip whitespace
            while (this.position < this.length && Char.IsWhiteSpace(currentChar)) // Uli: removed \v as Java doesn't support it
            { 
                ++this.position;
                if (this.position >= this.length)
                {
                    return null; // Uli: JS relies on charAt returns null
                }
                currentChar = this.path[this.position];
            }
            // skip comments
            if (currentChar == '/' && this.position + 1 < this.length && this.path[this.position + 1] == '*') 
            {
                int commentStart = this.position;
                this.position += 2;
                while (true)
                {
                    if (this.position + 1 >= this.length)
                    {
                        // no closing tag
                        throw new JException("S0106", commentStart);
                    }
                    if (this.path[this.position] == '*' && this.path[this.position + 1] == '/')
                    {
                        //found closing tag
                        this.position += 2;
                        return this.next(prefix); // need this to swallow any following whitespace
                    }
                    ++this.position;
                }
            }
                // test for regex
            if (prefix != true && currentChar == '/') 
            {
                this.position++;
                return this.create(SymbolType.regex, this.scanRegex());
            }
            // handle double-char operators
            bool haveMore = this.position < this.path.Length - 1; // Java: position+1 is valid
            if (currentChar == '.' && haveMore && this.path[this.position + 1] == '.') 
            {
                // double-dot .. range operator
                this.position += 2;
                return create(SymbolType.@operator, "..");
            }
            if (currentChar == ':' && haveMore && this.path[this.position + 1] == '=') 
            {
                // := assignment
                this.position += 2;
                return create(SymbolType.@operator, ":=");
            }
            if (currentChar == '!' && haveMore && this.path[this.position + 1] == '=') 
            {
                // !=
                this.position += 2;
                return create(SymbolType.@operator, "!=");
            }
            if (currentChar == '>' && haveMore && this.path[this.position + 1] == '=') 
            {
                // >=
                this.position += 2;
                return create(SymbolType.@operator, ">=");
            }
            if (currentChar == '<' && haveMore && this.path[this.position + 1] == '=') 
            {
                // <=
                this.position += 2;
                return create(SymbolType.@operator, "<=");
            }
            if (currentChar == '*' && haveMore && this.path[this.position + 1] == '*') 
            {
                // **  descendant wildcard
                this.position += 2;
                return create(SymbolType.@operator, "**");
            }
            if (currentChar == '~' && haveMore && this.path[this.position + 1] == '>') 
            {
                // ~>  chain function
                this.position += 2;
                return create(SymbolType.@operator, "~>");
            }
            if (currentChar == '?' && haveMore && this.path[this.position + 1] == ':') 
            {
                // ?: default / elvis operator
                this.position += 2;
                return create(SymbolType.@operator, "?:");
            }
            if (currentChar == '?' && haveMore && this.path[this.position + 1] == '?') 
            {
                // ?? coalescing operator
                this.position += 2;
                return create(SymbolType.@operator, "??");
            }
            // test for single char operators
            string currentCharAsStr = currentChar.ToString();
            if (Tokenizer.operators.ContainsKey(currentCharAsStr) )
            {
                this.position++;
                return create(SymbolType.@operator, currentCharAsStr);
            }
            // test for string literals
            if (currentChar == '"' || currentChar == '\'') 
            {
                char quoteType = currentChar;
                // double quoted string literal - find end of string
                ++this.position;
                string qstr = "";
                while (position < length) 
                {
                    currentChar = this.path[this.position];
                    if (currentChar == '\\')
                    { // escape sequence
                        this.position++;
                        currentChar = this.path[this.position];
                        if (Tokenizer.escapes.TryGetValue(currentChar.ToString(), out string? escaped)) 
                        {
                            qstr += escaped;
                        } 
                        else if (currentChar == 'u') 
                        {
                            //  u should be followed by 4 hex digits
                            string octets = this.path.Substring(this.position + 1, 4);
                            if (Regex.Match(octets, @"^[0-9a-fA-F]+$").Success)   //  /^[0-9a-fA-F]+$/.test(octets)) {
                            { 
                                int codepoint = Int32.Parse(octets, System.Globalization.NumberStyles.HexNumber);
                                qstr += ((char)codepoint).ToString();
                                this.position += 4;
                            } 
                            else 
                            {
                                throw new JException("S0104", this.position);
                            }
                        } 
                        else 
                        {
                            // illegal escape sequence
                            throw new JException("S0301", this.position, currentChar);
                        }
                    } 
                    else if (currentChar == quoteType) 
                    {
                        ++this.position;
                        return create(SymbolType.@string, qstr);
                    } 
                    else 
                    {
                        qstr += currentChar;
                    }
                    ++this.position;
                }
                throw new JException("S0101", position);
            }
            // test for numbers
            Match match = s_numregex.Match(this.path.Substring(position));
            if (match.Success) 
            {
                if (Int64.TryParse(match.Groups[0].Value, out long longValue))
                {
                    position += match.Groups[0].Value.Length;
                    return create(SymbolType.number, longValue);
                }
                else if (Double.TryParse(match.Groups[0].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
                {
                    if (!Double.IsNaN(doubleValue) && !Double.IsInfinity(doubleValue))
                    {
                        position += match.Groups[0].Value.Length;
                        // If the number is integral, use long as type
                        try
                        {
                            longValue = (long)doubleValue;
                            if (longValue == doubleValue)
                            {
                                return create(SymbolType.number, longValue);
                            }
                        }
                        catch (Exception)
                        {
                            //failed to cast double to long, it's ok
                        }
                        return create(SymbolType.number, doubleValue);
                    }
                    else
                    {
                        throw new JException("S0102", position); //, match.group[0]);
                    }
                }
                else
                {
                    throw new JException("S0102", position); //, match.group[0]);
                }
            }

            // test for quoted names (backticks)
            String name;
            if (currentChar == '`') 
            {
                // scan for closing quote
                ++this.position;
                int end = this.path.IndexOf('`', this.position);
                if (end != -1) 
                {
                    name = this.path.Substring(this.position, end - this.position);
                    this.position = end + 1;
                    return create(SymbolType.name, name);
                }
                this.position = this.length;
                throw new JException("S0105", this.position);
            }
            // test for names
            int i = this.position;
            char ch;
            while (true) 
            {
                //if (i>=length) return null; // Uli: JS relies on charAt returns null

                ch = i < this.length ? this.path[i] : (char)0;
                if (i == this.length || Char.IsWhiteSpace(ch) || Tokenizer.operators.ContainsKey(ch.ToString())) 
                {
                    if (this.path[this.position] == '$') 
                    {
                        // variable reference
                        String _name = this.path.Substring(this.position + 1, i - (this.position + 1));
                        this.position = i;
                        return create(SymbolType.variable, _name);
                    } 
                    else 
                    {
                        String _name = this.path.Substring(this.position, i - this.position);
                        this.position = i;
                        switch (_name) 
                        {
                        case "or":
                        case "in":
                        case "and":
                            return create(SymbolType.@operator, _name);
                        case "true":
                            return create(SymbolType.value, true);
                        case "false":
                            return create(SymbolType.value, false);
                        case "null":
                            return create(SymbolType.value, null);
                        default:
                            if (this.position == this.length && _name == "") 
                            {
                                // whitespace at end of input
                                return null;
                            }
                            return create(SymbolType.name, _name);
                        }
                    }
                } 
                else 
                {
                    ++i;
                }
            }
        }
    }
}
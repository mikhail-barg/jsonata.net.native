using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Jsonata.Net.Native.Impl
{

    public sealed class JException : Exception
    {
        public readonly string error;
        public readonly int location;
        object? current;
        object? expected;


        //TODO:
        public JException(string error, string message)
            : this(error + ": " + message, -1, null, null)
        {
        }

        public JException(string error) 
            : this(error, -1, null, null)
        {
        }
        
        public JException(string error, int location) 
            : this(error, location, null, null)
        {
            
        }
        public JException(string error, int location, object? currentToken)
            : this(error, location, currentToken, null)
        {
        }
        public JException(string error, int location, object? currentToken, object? expected) 
            : this(null, error, location, currentToken, expected)
        {
        }
        public JException(Exception? cause, string error, int location, object? currentToken, object? expected) 
            :base(msg(error, location, currentToken, expected), cause)
        {
            
            this.error = error; 
            this.location = location;
            this.current = currentToken;
            this.expected = expected;
        }

        /**
         * Returns the error code, i.e. S0201
         * @return
         */
        public string getError() {
            return this.error;
        }

        /**
         * Returns the error location (in characters)
         * @return
         */
        public int getLocation() {
            return this.location;
        }

        /**
         * Returns the current token
         * @return
         */
        public object? getCurrent() {
            return this.current;
        }

        /**
         * Returns the expected token
         * @return
         */
        public object? getExpected() {
            return this.expected;
        }

        /**
         * Returns the error message with error details in the text.
         * Example: Syntax error: ")" {code=S0201 position=3}
         * @return
         */
        public string getDetailedErrorMessage() {
            return msg(this.error, this.location, this.current, this.expected, true);
        }

        /**
         * Generate error message from given error code
         * Codes are defined in Jsonata.errorCodes
         * 
         * Fallback: if error code does not exist, return a generic message
         * 
         * @param error
         * @param location
         * @param arg1
         * @param arg2
         * @return
         */
        public static string msg(string error, int location, object? arg1, object? arg2) 
        {
            return msg(error, location, arg1, arg2, true);
        }

        /**
         * Generate error message from given error code
         * Codes are defined in Jsonata.errorCodes
         * 
         * Fallback: if error code does not exist, return a generic message
         * 
         * @param error
         * @param location
         * @param arg1
         * @param arg2
         * @param details True = add error details as text, false = don't add details (use getters to retrieve details)
         * @return
         */
        //TODO: use proper formatting, as in jsonata.js populateMessage()
        private static readonly Regex s_replaceRegex = new Regex(@"\{\{\w+\}\}", RegexOptions.Compiled);
        public static string msg(string error, int location, object? arg1, object? arg2, bool details) 
        {
            if (!s_errorCodes.TryGetValue(error, out string? message)) 
            {
                // unknown error code
                return "JsonataException " + error +
                    (details ? $" {{code=unknown, position={location}, arg1={arg1}, arg2={arg2}}}" : "");
            }

            //TODO: 

            string formatted = message;
            try 
            {
                // Replace any {{var}} with Java format "%1$s"
                formatted = s_replaceRegex.Replace(formatted, "{0}", 1);
                formatted = s_replaceRegex.Replace(formatted, "{1}", 1);
                formatted = String.Format(formatted, arg1, arg2);
            } 
            catch (FormatException) 
            {
                //TODO: fix
                formatted = $"{message} (message formatting error, args are {arg1}, {arg2})";
            }
            if (details) 
            {
                formatted = formatted + " {code=" + error;
                if (location >= 0)
                {
                    formatted += ", position=" + location;
                }
                formatted += "}";
            }
            return formatted;
        }

        /**
         * Error codes
         *
         * Sxxxx    - Static errors (compile time)
         * Txxxx    - Type errors
         * Dxxxx    - Dynamic errors (evaluate time)
         *  01xx    - tokenizer
         *  02xx    - parser
         *  03xx    - regex parser
         *  04xx    - object signature parser/evaluator
         *  10xx    - evaluator
         *  20xx    - operators
         *  3xxx    - functions (blocks of 10 for each function)
         */
        private static readonly Dictionary<string, string> s_errorCodes = new() {
             { "S0101", "string literal must be terminated by a matching quote" },
             { "S0102", "Number out of range: {{token}}" },
             { "S0103", "Unsupported escape sequence: \\{{token}}" },
             { "S0104", "The escape sequence \\u must be followed by 4 hex digits" },
             { "S0105", "Quoted property name must be terminated with a backquote (`)" },
             { "S0106", "Comment has no closing tag" },
             { "S0201", "Syntax error: {{token}}" },
             { "S0202", "Expected {{value}}, got {{token}}" },
             { "S0203", "Expected {{value}} before end of expression" },
             { "S0204", "Unknown operator: {{token}}" },
             { "S0205", "Unexpected token: {{token}}" },
             { "S0206", "Unknown expression type: {{token}}" },
             { "S0207", "Unexpected end of expression" },
             { "S0208", "Parameter {{value}} of object definition must be a variable name (start with $)" },
             { "S0209", "A predicate cannot follow a grouping expression in a step" },
             { "S0210", "Each step can only have one grouping expression" },
             { "S0211", "The symbol {{token}} cannot be used as a unary operator" },
             { "S0212", "The left side of := must be a variable name (start with $)" },
             { "S0213", "The literal value {{value}} cannot be used as a step within a path expression" },
             { "S0214", "The right side of {{token}} must be a variable name (start with $)" },
             { "S0215", "A context variable binding must precede any predicates on a step" },
             { "S0216", "A context variable binding must precede the \"order-by\" clause on a step" },
             { "S0217", "The object representing the \"parent\" cannot be derived from this expression" },
             { "S0301", "Empty regular expressions are not allowed" },
             { "S0302", "No terminating / in regular expression" },
             { "S0402", "Choice groups containing parameterized types are not supported" },
             { "S0401", "Type parameters can only be applied to functions and arrays" },
             { "S0500", "Attempted to evaluate an expression containing syntax error(s)" },
             { "T0410", "Argument {{index}} of object {{token}} does not match object signature" },
             { "T0411", "Context value is not a compatible type with argument {{index}} of object {{token}}" },
             { "T0412", "Argument {{index}} of object {{token}} must be an array of {{type}}" },
             { "D1001", "Number out of range: {{value}}" },
             { "D1002", "Cannot negate a non-numeric value: {{value}}" },
             { "T1003", "Key in object structure must evaluate to a string; got: {{value}}" },
             { "D1004", "Regular expression matches zero length string" },
             { "T1005", "Attempted to invoke a non-function. Did you mean ${{{token}}}?" },
             { "T1006", "Attempted to invoke a non-function" },
             { "T1007", "Attempted to partially apply a non-function. Did you mean ${{{token}}}?" },
             { "T1008", "Attempted to partially apply a non-function" },
             { "D1009", "Multiple key definitions evaluate to same key: {{value}}" },
             { "T1010", "The matcher object argument passed to object {{token}} does not return the correct object structure" },
             { "T2001", "The left side of the {{token}} operator must evaluate to a number" },
             { "T2002", "The right side of the {{token}} operator must evaluate to a number" },
             { "T2003", "The left side of the range operator (..) must evaluate to an integer" },
             { "T2004", "The right side of the range operator (..) must evaluate to an integer" },
             { "D2005", "The left side of := must be a variable name (start with $)" },  // defunct - replaced by S0212 parser error
             { "T2006", "The right side of the object application operator ~> must be a function" },
             { "T2007", "Type mismatch when comparing values {{value}} and {{value2}} in order-by clause" },
             { "T2008", "The expressions within an order-by clause must evaluate to numeric or string values" },
             { "T2009", "The values {{value}} and {{value2}} either side of operator {{token}} must be of the same data type" },
             { "T2010", "The expressions either side of operator {{token}} must evaluate to numeric or string values" },
             { "T2011", "The insert/update clause of the transform expression must evaluate to an object: {{value}}" },
             { "T2012", "The delete clause of the transform expression must evaluate to a string or array of strings: {{value}}" },
             { "T2013", "The transform expression clones the input object using the $clone() function.  This has been overridden in the current scope by a non-function." },
             { "D2014", "The size of the sequence allocated by the range operator (..) must not exceed 1e6.  Attempted to allocate {{value}}." },
             { "D3001", "Attempting to invoke string object on Infinity or NaN" },
             { "D3010", "Second argument of replace object cannot be an empty string" },
             { "D3011", "Fourth argument of replace object must evaluate to a positive number" },
             { "D3012", "Attempted to replace a matched string with a non-string value" },
             { "D3020", "Third argument of split object must evaluate to a positive number" },
             { "D3030", "Unable to cast value to a number: {{value}}" },
             { "D3040", "Third argument of match object must evaluate to a positive number" },
             { "D3050", "The second argument of reduce object must be a object with at least two arguments" },
             { "D3060", "The sqrt object cannot be applied to a negative number: {{value}}" },
             { "D3061", "The power object has resulted in a value that cannot be represented as a JSON number: base={{value}}, exponent={{exp}}" },
             { "D3070", "The single argument form of the sort object can only be applied to an array of strings or an array of numbers.  Use the second argument to specify a comparison function" },
             { "D3080", "The picture string must only contain a maximum of two sub-pictures" },
             { "D3081", "The sub-picture must not contain more than one instance of the \"decimal-separator\" character" },
             { "D3082", "The sub-picture must not contain more than one instance of the \"percent\" character" },
             { "D3083", "The sub-picture must not contain more than one instance of the \"per-mille\" character" },
             { "D3084", "The sub-picture must not contain both a \"percent\" and a \"per-mille\" character" },
             { "D3085", "The mantissa part of a sub-picture must contain at least one character that is either an \"optional digit character\" or a member of the \"decimal digit family\"" },
             { "D3086", "The sub-picture must not contain a passive character that is preceded by an active character and that is followed by another active character" },
             { "D3087", "The sub-picture must not contain a \"grouping-separator\" character that appears adjacent to a \"decimal-separator\" character" },
             { "D3088", "The sub-picture must not contain a \"grouping-separator\" at the end of the integer part" },
             { "D3089", "The sub-picture must not contain two adjacent instances of the \"grouping-separator\" character" },
             { "D3090", "The integer part of the sub-picture must not contain a member of the \"decimal digit family\" that is followed by an instance of the \"optional digit character\"" },
             { "D3091", "The fractional part of the sub-picture must not contain an instance of the \"optional digit character\" that is followed by a member of the \"decimal digit family\"" },
             { "D3092", "A sub-picture that contains a \"percent\" or \"per-mille\" character must not contain a character treated as an \"exponent-separator\"" },
             { "D3093", "The exponent part of the sub-picture must comprise only of one or more characters that are members of the \"decimal digit family\"" },
             { "D3100", "The radix of the formatBase object must be between 2 and 36.  It was given {{value}}" },
             { "D3110", "The argument of the toMillis object must be an ISO 8601 formatted timestamp. Given {{value}}" },
             { "D3120", "Syntax error in expression passed to object eval: {{value}}" },
             { "D3121", "Dynamic error evaluating the expression passed to object eval: {{value}}" },
             { "D3130", "Formatting or parsing an integer as a sequence starting with {{value}} is not supported by this implementation" },
             { "D3131", "In a decimal digit pattern, all digits must be from the same decimal group" },
             { "D3132", "Unknown component specifier {{value}} in date/time picture string" },
             { "D3133", "The \"name\" modifier can only be applied to months and days in the date/time picture string, not {{value}}" },
             { "D3134", "The timezone integer format specifier cannot have more than four digits" },
             { "D3135", "No matching closing bracket \"]\" in date/time picture string" },
             { "D3136", "The date/time picture string is missing specifiers required to parse the timestamp" },
             { "D3137", "{{{message}}}" },
             { "D3138", "The $single() object expected exactly 1 matching result.  Instead it matched more." },
             { "D3139", "The $single() object expected exactly 1 matching result.  Instead it matched 0." },
             { "D3140", "Malformed URL passed to ${{{functionName}}}(): {{value}}" },
             { "D3141", "{{{message}}}" },
        };
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Jsonata.Net.Native.Json;

namespace Jsonata.Net.Native.Impl
{
    public sealed class Signature : IEquatable<Signature>
    {
        private sealed class Param
        {
            internal string? regex;
            internal string? type;
            internal bool array;
            internal bool context;
            internal string? subtype;
            internal Regex? contextRegex;
        }

        private readonly string m_signature;
        private readonly Regex m_regex;
        private readonly List<Param> m_params;

        internal Signature(string signature)
        {
            this.m_signature = signature;

            // create a Regex that represents this signature and return a function that when invoked,
            // returns the validated (possibly fixed-up) arguments, or throws a validation error
            // step through the signature, one symbol at a time

            int position = 1;
            List<Param> @params = new();
            Param param = new Param();
            Param prevParam = param;

            void next()
            {
                @params.Add(param);
                prevParam = param;
                param = new Param();
            }

            while (position < signature.Length)
            {
                char symbol = signature[position];
                if (symbol == ':')
                {
                    // TODO figure out what to do with the return type
                    // ignore it for now
                    break;
                }

                switch (symbol)
                {
                case 's': // string
                case 'n': // number
                case 'b': // boolean
                case 'l': // not so sure about expecting null?
                case 'o': // object
                    param.regex = "[" + symbol + "m]";
                    param.type = symbol.ToString();
                    next();
                    break;
                case 'a': // array
                    //  normally treat any value as singleton array
                    param.regex = "[asnblfom]";
                    param.type = symbol.ToString();
                    param.array = true;
                    next();
                    break;
                case 'f': // function
                    param.regex = "f";
                    param.type = symbol.ToString();
                    next();
                    break;
                case 'j': // any JSON type
                    param.regex = "[asnblom]";
                    param.type = symbol.ToString();
                    next();
                    break;
                case 'x': // any type
                    param.regex = "[asnblfom]";
                    param.type = symbol.ToString();
                    next();
                    break;
                case '-': // use context if param not supplied
                    prevParam.context = true;
                    prevParam.contextRegex = new Regex(prevParam.regex!, RegexOptions.Compiled); // pre-compiled to test the context type at runtime
                    prevParam.regex += '?';
                    break;
                case '?': // optional param
                case '+': // one or more
                    prevParam.regex += symbol;
                    break;
                case '(': // choice of types
                    // search forward for matching ')'
                    int endParen = FindClosingBracket(signature, position, '(', ')');
                    string choice = signature.Substring(position + 1, endParen - (position + 1));
                    if (choice.IndexOf('<') < 0)
                    {
                        // no parameterized types, simple regex
                        param.regex = "[" + choice + "m]";
                    }
                    else
                    {
                        // TODO harder
                        throw new JsonataException(JsonataErrorCode.S0402, $"Choice groups containing parameterized types are not supported: '{choice}'", position);
                    }
                    param.type = "(" + choice + ")";
                    position = endParen;
                    next();
                    break;
                case '<': // type parameter - can only be applied to 'a' and 'f'
                    if (prevParam.type == "a" || prevParam.type == "f")
                    {
                        // search forward for matching '>'
                        int endPos = FindClosingBracket(signature, position, '<', '>');
                        prevParam.subtype = signature.Substring(position + 1, endPos - (position + 1));
                        position = endPos;
                    }
                    else
                    {
                        throw new JsonataException(JsonataErrorCode.S0401, $"Type parameters can only be applied to functions and arrays: {prevParam.type}", position);
                    }
                    break;
                }
                ++position;
            }

            StringBuilder regexStr = new StringBuilder();
            regexStr.Append('^');
            foreach (Param p in @params)
            {
                regexStr.Append('(').Append(p.regex).Append(')');
            }
            regexStr.Append('$');
            Regex regex = new Regex(regexStr.ToString(), RegexOptions.Compiled);
            this.m_regex = regex;
            this.m_params = @params;
        }



        private static int FindClosingBracket(string str, int start, char openSymbol, char closeSymbol)
        {
            // returns the position of the closing symbol (e.g. bracket) in a string
            // that balances the opening symbol at position start
            int depth = 1;
            int position = start;
            while (position < str.Length - 1)
            {
                position++;
                char symbol = str[position];
                if (symbol == closeSymbol)
                {
                    --depth;
                    if (depth == 0)
                    {
                        // we're done
                        break; // out of while loop
                    }
                }
                else if (symbol == openSymbol)
                {
                    ++depth;
                }
            }
            return position;
        }

        private static char GetSymbol(JToken value)
        {
            switch (value.Type)
            {
            case JTokenType.Function:
                return 'f';
            case JTokenType.String:
                return 's';
            case JTokenType.Float:
            case JTokenType.Integer:
                return 'n';
            case JTokenType.Boolean:
                return 'b';
            case JTokenType.Null:
                return 'l';
            case JTokenType.Array:
                return 'a';
            case JTokenType.Object:
                return 'o';
            case JTokenType.Undefined:
            default:
                // any value can be undefined, but should be allowed to match
                return 'm'; // m for missing
            }
        }

        private static Dictionary<string, string> s_arraySignatureMapping = new() {
            { "a", "arrays" },
            { "b", "booleans" },
            { "f", "functions" },
            { "n", "numbers" },
            { "o", "objects" },
            { "s", "strings" }
        };

        internal List<JToken> Validate(List<JToken> args, JToken context, string functionName)
        {
            StringBuilder builder = new StringBuilder();
            foreach (JToken arg in args)
            {
                builder.Append(GetSymbol(arg));
            }
            string suppliedSig = builder.ToString();
            Match isValid = this.m_regex.Match(suppliedSig);
            if (isValid.Success)
            {
                List<JToken> validatedArgs = new();
                int argIndex = 0;
                for (int index = 0; index < this.m_params.Count; ++index)
                {
                    Param param = this.m_params[index];
                    JToken arg;
                    if (argIndex < args.Count)
                    {
                        arg = args[argIndex];
                    }
                    else
                    {
                        arg = JsonataQ.UNDEFINED;
                    }
                    Group match = isValid.Groups[index + 1];
                    if (match.Value == "")
                    {
                        if (param.context && param.contextRegex != null)
                        {
                            // substitute context value for missing arg
                            // first check that the context value is the right type
                            string contextType = GetSymbol(context).ToString();
                            // test contextType against the regex for this arg (without the trailing ?)
                            if (param.contextRegex.IsMatch(contextType))
                            {
                                validatedArgs.Add(context);
                            }
                            else
                            {
                                // context value not compatible with this argument
                                throw new JsonataException(JsonataErrorCode.T0411, $"Context value is not a compatible type with argument {argIndex + 1} of function {functionName}");
                            }
                        }
                        else
                        {
                            validatedArgs.Add(arg);
                            ++argIndex;
                        }
                    }
                    else
                    {
                        // may have matched multiple args (if the regex ends with a '+')
                        // split into single tokens
                        foreach (char single in match.Value)
                        {
                            if (param.type == "a")
                            {
                                if (single == 'm')
                                {
                                    // missing (undefined)
                                    arg = JsonataQ.UNDEFINED;
                                }
                                else
                                {
                                    arg = args[argIndex];
                                    bool arrayOK = true;
                                    // is there type information on the contents of the array?
                                    if (param.subtype != null)
                                    {
                                        if (single != 'a' && match.Value != param.subtype)
                                        {
                                            arrayOK = false;
                                        }
                                        else if (single == 'a')
                                        {
                                            JArray argArray = (JArray)arg;
                                            if (argArray.Count > 0)
                                            {
                                                char itemType = GetSymbol(argArray.ChildrenTokens[0]);
                                                if (itemType != param.subtype[0])
                                                { // TODO recurse further
                                                    arrayOK = false;
                                                }
                                                else
                                                {
                                                    for (int i = 1; i < argArray.Count; ++i)
                                                    {
                                                        if (GetSymbol(argArray.ChildrenTokens[i]) != itemType)
                                                        {
                                                            arrayOK = false;
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (!arrayOK)
                                    {
                                        throw new JsonataException(JsonataErrorCode.T0412, $"Argument {argIndex + 1} of function {functionName} must be an array of {s_arraySignatureMapping[param.subtype!]}");
                                    }

                                    if (single != 'a')
                                    {
                                        // the function expects an array. If it's not one, make it so
                                        JArray arrayArg = new JArray();
                                        arrayArg.Add(arg);
                                        arg = arrayArg;
                                    }
                                }
                                validatedArgs.Add(arg);
                                ++argIndex;
                            }
                            else
                            {
                                validatedArgs.Add(arg);
                                ++argIndex;
                            }
                        }
                    }
                }
                return validatedArgs;
            }
            else
            {
                // TODO: implement proper error search
                throw new JsonataException(JsonataErrorCode.T0410, $"Some argument of function {functionName} does not match function signature");

                // throwValidationError(args, suppliedSig);
                // var throwValidationError = function (badArgs, badSig) {

                // to figure out where this went wrong we need apply each component of the
                // regex to each argument until we get to the one that fails to match
                /*
                StringBuilder partialPattern = new StringBuilder();
                partialPattern.Append('^');
                int goodTo = 0;
                for (int index = 0; index < this.m_params.Count; ++index) 
                {
                    partialPattern.Append(this.m_params[index].regex);
                    var match = badSig.match(partialPattern);
                    if (match === null) {
                        // failed here
                        throw {
                            code: "T0410",
                            stack: (new Error()).stack,
                            value: badArgs[goodTo],
                            index: goodTo + 1
                        };
                    }
                    goodTo = match[0].length;
                }
                // if it got this far, it's probably because of extraneous arguments (we
                // haven't added the trailing '$' in the regex yet.
                throw {
                    code: "T0410",
                    stack: (new Error()).stack,
                    value: badArgs[goodTo],
                    index: goodTo + 1
                };// to figure out where this went wrong we need apply each component of the
                // regex to each argument until we get to the one that fails to match
                var partialPattern = '^';
                var goodTo = 0;
                for (var index = 0; index < params.length; index++) {
                    partialPattern += params[index].regex;
                    var match = badSig.match(partialPattern);
                    if (match === null) {
                        // failed here
                        throw {
                            code: "T0410",
                            stack: (new Error()).stack,
                            value: badArgs[goodTo],
                            index: goodTo + 1
                        };
                    }
                    goodTo = match[0].length;
                }
                // if it got this far, it's probably because of extraneous arguments (we
                // haven't added the trailing '$' in the regex yet.
                throw {
                    code: "T0410",
                    stack: (new Error()).stack,
                    value: badArgs[goodTo],
                    index: goodTo + 1
                };
                }
                */
            }
        }

        public override string ToString()
        {
            return this.m_signature;
        }

        public bool Equals(Signature? other)
        {
            if (other == null)
            {
                return false;
            }
            return this.m_signature == other.m_signature;
        }
    }
}

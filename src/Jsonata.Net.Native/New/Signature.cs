using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.New
{
    internal class Signature
    {
        /*
        static class Param
        {
            String type;
            String regex;
            boolean context;
            boolean array;
            String subtype;
            String contextRegex;

            @Override
            public String toString()
            {
                return "Param " + type + " regex=" + regex + " ctx=" + context + " array=" + array;
            }
        }

        Param _param = new Param();

        List<Param> _params = new ArrayList<>();
        Param _prevParam = _param;
        Pattern _regex = null;
        String _signature = "";
        String functionName;
        */

        public Signature(String signature, String function)
        {
            //TODO:
            //this.functionName = function;
            //parseSignature(signature);
        }

        /*
        public void setFunctionName(String functionName)
        {
            this.functionName = functionName;
        }

        public static void main(String[] args)
        {
            Signature s = new Signature("<s-:s>", "test");//<s-(sf)(sf)n?:s>");
            System.out.println(s._params);
        }

        int findClosingBracket(String str, int start, char openSymbol, char closeSymbol)
        {
            // returns the position of the closing symbol (e.g. bracket) in a string
            // that balances the opening symbol at position start
            int depth = 1;
            int position = start;
            while (position < str.length())
            {
                position++;
                char symbol = str.charAt(position);
                if (symbol == closeSymbol)
                {
                    depth--;
                    if (depth == 0)
                    {
                        // we're done
                        break; // out of while loop
                    }
                }
                else if (symbol == openSymbol)
                {
                    depth++;
                }
            }
            return position;
        };

        String getSymbol(Object value)
        {
            String symbol;
            if (value == null)
            {
                symbol = "m";
            }
            else
            {
                // first check to see if this is a function
                if (Utils.isFunction(value) || Functions.isLambda(value) || (value instanceof Pattern)) { //} instanceof JFunction || value instanceof Function) {
                    symbol = "f";
                } else if (value instanceof String)
                symbol = "s";
            else if (value instanceof Number)
                symbol = "n";
            else if (value instanceof Boolean)
                symbol = "b";
            else if (value instanceof List)
                symbol = "a";
            else if (value instanceof Map)
                symbol = "o";
            else if (value instanceof NullType) // Uli: is this used???
                 symbol = "l";
            else
                    // any value can be undefined, but should be allowed to match
                    symbol = "m"; // m for missing
            }
            return symbol;
        };

        void next()
        {
            _params.add(_param);
            _prevParam = _param;
            _param = new Param();
        }

        **
         * Parses a function signature definition and returns a validation function
         * 
         * @param {string}
         *                 signature - the signature between the <angle brackets>
         * @returns validation pattern
         *
        Pattern parseSignature(String signature)
        {
            // create a Regex that represents this signature and return a function that when
            // invoked,
            // returns the validated (possibly fixed-up) arguments, or throws a validation
            // error
            // step through the signature, one symbol at a time
            int position = 1;
            while (position < signature.length())
            {
                char symbol = signature.charAt(position);
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
                case 'o':
                    { // object
                        _param.regex = ("[" + symbol + "m]");
                        _param.type = ("" + symbol);
                        next();
                        break;
                    }
                case 'a':
                    { // array
                      // normally treat any value as singleton array
                        _param.regex = ("[asnblfom]");
                        _param.type = ("" + symbol);
                        _param.array = (true);
                        next();
                        break;
                    }
                case 'f':
                    { // function
                        _param.regex = ("f");
                        _param.type = ("" + symbol);
                        next();
                        break;
                    }
                case 'j':
                    { // any JSON type
                        _param.regex = ("[asnblom]");
                        _param.type = ("" + symbol);
                        next();
                        break;
                    }
                case 'x':
                    { // any type
                        _param.regex = ("[asnblfom]");
                        _param.type = ("" + symbol);
                        next();
                        break;
                    }
                case '-':
                    { // use context if _param not supplied
                        _prevParam.context = true;
                        _prevParam.regex += "?";
                        break;
                    }
                case '?': // optional _param
                case '+':
                    { // one or more
                        _prevParam.regex += symbol;
                        break;
                    }
                case '(':
                    { // choice of types
                      // search forward for matching ')'
                        int endParen = findClosingBracket(signature, position, '(', ')');
                        String choice = signature.substring(position + 1, endParen);
                        if (choice.indexOf("<") == -1)
                        {
                            // no _parameterized types, simple regex
                            _param.regex = ("[" + choice + "m]");
                        }
                        else
                        {
                            // TODO harder
                            throw new RuntimeException("Choice groups containing parameterized types are not supported");
                        }
                        _param.type = ("(" + choice + ")");
                        position = endParen;
                        next();
                        break;
                    }
                case '<':
                    { // type _parameter - can only be applied to 'a' and 'f'
                        String test = _prevParam.type;
                        if (test != null)
                        {
                            String type = test;//.asText();
                            if (type.equals("a") || type.equals("f"))
                            {
                                // search forward for matching '>'
                                int endPos = findClosingBracket(signature, position, '<', '>');
                                _prevParam.subtype = signature.substring(position + 1, endPos);
                                position = endPos;
                            }
                            else
                            {
                                throw new RuntimeException("Type parameters can only be applied to functions and arrays");
                            }
                        }
                        else
                        {
                            throw new RuntimeException("Type parameters can only be applied to functions and arrays");
                        }
                        break;
                    }
                }
                position++;
            } // end while processing symbols in signature

            String regexStr = "^";
            for (Param param : _params)
            {
                regexStr += "(" + param.regex + ")";
            }
            regexStr += "$";

            _regex = null;
            try
            {
                _regex = Pattern.compile(regexStr);
                _signature = regexStr;
            }
            catch (PatternSyntaxException pse)
            {
                throw new RuntimeException(pse.getLocalizedMessage());
            }
            return _regex;
        }

        void throwValidationError(List<?> badArgs, String badSig, String functionName)
        {
            // to figure out where this went wrong we need apply each component of the
            // regex to each argument until we get to the one that fails to match
            String partialPattern = "^";

            int goodTo = 0;
            for (int index = 0; index < _params.size(); index++)
            {
                partialPattern += _params.get(index).regex;
                Pattern tester = Pattern.compile(partialPattern);
                Matcher match = tester.matcher(badSig);
                if (match.matches() == false)
                {
                    // failed here
                    throw new JException("T0410", -1, (goodTo + 1), functionName);
                }
                goodTo = match.end();
            }
            // if it got this far, it's probably because of extraneous arguments (we
            // haven't added the trailing '$' in the regex yet.
            throw new JException("T0410", -1, (goodTo + 1), functionName);
        }

        @SuppressWarnings({ "rawtypes", "unchecked"})
    public Object validate(Object _args, Object context)
        {

            var result = new ArrayList<>();

            var args = (List)_args;
            String suppliedSig = "";
            for (Object arg : args)
                suppliedSig += getSymbol(arg);

            Matcher isValid = _regex.matcher(suppliedSig);
            if (isValid != null && isValid.matches())
            {
                var validatedArgs = new ArrayList<>();
                var argIndex = 0;
                int index = 0;
                for (Object _param : _params)
                {
                    Param param = (Param)_param;
                    var arg = argIndex < args.size() ? args.get(argIndex) : null;
                    String match = isValid.group(index + 1);
                    if ("".equals(match))
                    {
                        if (param.context && param.regex != null)
                        {
                            // substitute context value for missing arg
                            // first check that the context value is the right type
                            var contextType = getSymbol(context);
                            // test contextType against the regex for this arg (without the trailing ?)
                            if (Pattern.matches(param.regex, contextType))
                            {
                                //if (param.contextRegex.test(contextType)) {
                                validatedArgs.add(context);
                            }
                            else
                            {
                                // context value not compatible with this argument
                                throw new JException("T0411", -1,
                                    context,
                                    argIndex + 1
                                );
                            }
                        }
                        else
                        {
                            validatedArgs.add(arg);
                            argIndex++;
                        }
                    }
                    else
                    {
                        // may have matched multiple args (if the regex ends with a '+'
                        // split into single tokens
                        String[] singles = match.split("");
                        for (String single : singles)
                        {
                            //match.split('').forEach(function (single) {
                            if (param.type.equals("a"))
                            {
                                if (single.equals("m"))
                                {
                                    // missing (undefined)
                                    arg = null;
                                }
                                else
                                {
                                    arg = argIndex < args.size() ? args.get(argIndex) : null;
                                    var arrayOK = true;
                                    // is there type information on the contents of the array?
                                    if (param.subtype != null)
                                    {
                                        if (!single.equals("a") && !match.equals(param.subtype))
                                        {
                                            arrayOK = false;
                                        }
                                        else if (single.equals("a"))
                                        {
                                            List argArr = (List)arg;
                                            if (argArr.size() > 0)
                                            {
                                                var itemType = getSymbol(argArr.get(0));
                                                if (!itemType.equals("" + param.subtype.charAt(0)))
                                                { // TODO recurse further
                                                    arrayOK = false;
                                                }
                                                else
                                                {
                                                    // make sure every item in the array is this type
                                                    for (Object o : argArr)
                                                    {
                                                        if (!getSymbol(o).equals(itemType))
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
                                        throw new JException("T0412", -1,
                                            arg,
                                            //argIndex + 1,
                                            param.subtype//arraySignatureMapping[param.subtype]
                                        );
                                    }
                                    // the function expects an array. If it's not one, make it so
                                    if (!single.equals("a"))
                                    {
                                        List _arg = new ArrayList<>(); _arg.add(arg);
                                        arg = _arg;
                                    }
                                }
                                validatedArgs.add(arg);
                                argIndex++;
                            }
                            else
                            {
                                arg = argIndex < args.size() ? args.get(argIndex) : null;
                                validatedArgs.add(arg);
                                argIndex++;
                            }
                        }
                    }
                }
                return validatedArgs;
            }
            throwValidationError(args, suppliedSig, functionName);
            return null; // dead code -> compiler happy
        }

        public int getNumberOfArgs()
        {
            return _params.size();
        }

        **
         * Returns the minimum # of arguments.
         * I.e. the # of all non-optional arguments.
         *
        public int getMinNumberOfArgs()
        {
            int res = 0;
            for (Param p : _params)
                if (!p.regex.contains("?"))
                    res++;
            return res;
        }
        */
    }
}

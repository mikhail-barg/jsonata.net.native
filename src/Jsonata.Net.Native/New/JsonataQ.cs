using Jsonata.Net.Native.Json;
using Jsonata.Net.Native;
using System.Collections.Generic;
using System;
using Jsonata.Net.Native.Eval;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Jsonata.Net.Native.New 
{
    public class JsonataQ
    {
        internal static readonly JValue UNDEFINED = JValue.CreateUndefined();

        internal static JToken evaluate(Symbol expr, JToken input, EvaluationEnvironment environment)
        {
            JToken result;

            switch (expr.type)
            {
            case SymbolType.path:
                result = JsonataQ.evaluatePath(expr, input, environment);
                break;
            case SymbolType.binary:
                result = JsonataQ.evaluateBinary(expr, input, environment);
                break;
            case SymbolType.unary:
                result = JsonataQ.evaluateUnary(expr, input, environment);
                break;
            case SymbolType.name:
                result = JsonataQ.evaluateName(expr, input, environment);
                break;
            case SymbolType.@string:
            case SymbolType.number:
            case SymbolType.value:
                result = JsonataQ.evaluateLiteral(expr); //, input, environment);
                break;
            case SymbolType.wildcard:
                result = JsonataQ.evaluateWildcard(expr, input); //, environment);
                break;
            case SymbolType.descendant:
                result = JsonataQ.evaluateDescendants(expr, input); //, environment);
                break;
            case SymbolType.parent:
                result = environment.Lookup(expr.slot!.label!);
                break;
            case SymbolType.condition:
                result = JsonataQ.evaluateCondition(expr, input, environment);
                break;
            case SymbolType.block:
                result = JsonataQ.evaluateBlock(expr, input, environment);
                break;
            case SymbolType.bind:
                result = JsonataQ.evaluateBindExpression(expr, input, environment);
                break;
            case SymbolType.regex:
                result = JsonataQ.evaluateRegex(expr); //, input, environment);
                break;
            case SymbolType.function:
                result = JsonataQ.evaluateFunction(expr, input, environment, null);
                break;
            case SymbolType.variable:
                result = JsonataQ.evaluateVariable(expr, input, environment);
                break;
            case SymbolType.lambda:
                result = JsonataQ.evaluateLambda(expr, input, environment);
                break;
            case SymbolType.partial:
                result = JsonataQ.evaluatePartialApplication(expr, input, environment);
                break;
            case SymbolType.apply:
                result = JsonataQ.evaluateApplyExpression(expr, input, environment);
                break;
            case SymbolType.transform:
                result = JsonataQ.evaluateTransformExpression(expr, input, environment);
                break;
            default:
                //no throws here in jsonata-js
                result = JsonataQ.UNDEFINED;
                break;
            }

            if (expr.predicate != null)
            {
                foreach (Symbol element in expr.predicate)
                {
                    result = JsonataQ.evaluateFilter(element.expr!, result, environment);
                }
            }

            if ((expr.type != SymbolType.path) && expr.group != null)
            {
                result = JsonataQ.evaluateGroupExpression(expr.group, result, environment);
            }

            // mangle result (list of 1 element -> 1 element, empty list -> null)
            if ((result is JsonataArray arrayResult) && arrayResult.sequence && !arrayResult.tupleStream)
            {
                if (expr.keepArray)
                {
                    arrayResult.keepSingleton = true;
                }
                if (arrayResult.Count == 0)
                {
                    result = JsonataQ.UNDEFINED;
                }
                else if (arrayResult.Count == 1)
                {
                    result = arrayResult.keepSingleton ? arrayResult : arrayResult.ChildrenTokens[0];
                }
            }

            return result;
        }

        /**
         * Evaluate path expression against input data
        * @param {Object} expr - JSONata expression
        * @param {Object} input - Input data to evaluate against
        * @param {Object} environment - Environment
        * @returns {*} Evaluated input data
        */
        private static JToken evaluatePath(Symbol expr, JToken input, EvaluationEnvironment environment)
        {
            JArray inputSequence;
            // expr is an array of steps
            // if the first step is a variable reference ($...), including root reference ($$),
            //   then the path is absolute rather than relative
            if (input is JArray inputArray && expr.steps![0].type != SymbolType.variable)
            {
                inputSequence = inputArray;
            }
            else
            {
                // if input is not an array, make it so
                inputSequence = JsonataArray.CreateSequence(input);
            }

            JArray resultSequence = default!;   //to suppress unitinialized error later
            bool isTupleStream = false;

            JArray? tupleBindings = null;

            // evaluate each step in turn
            for (int ii = 0; ii < expr.steps!.Count; ++ii)
            {
                Symbol step = expr.steps[ii];

                if (step.tuple)
                {
                    isTupleStream = true;
                }

                // if the first step is an explicit array constructor, then just evaluate that (i.e. don't iterate over a context array)
                if (ii == 0 && step.consarray)
                {
                    resultSequence = (JArray)JsonataQ.evaluate(step, inputSequence, environment);
                }
                else
                {
                    if (isTupleStream)
                    {
                        tupleBindings = JsonataQ.evaluateTupleStep(step, inputSequence, tupleBindings, environment);
                    }
                    else
                    {
                        resultSequence = JsonataQ.evaluateStep(step, inputSequence, environment, ii == expr.steps.Count - 1);
                    }
                }

                if (!isTupleStream && (resultSequence.Type == JTokenType.Undefined || resultSequence.ChildrenTokens.Count == 0))
                {
                    break;
                }

                if (step.focus == null)
                {
                    inputSequence = resultSequence;
                }
            }

            if (isTupleStream)
            {
                if (tupleBindings == null)
                {
                    throw new Exception("Should not happen!");
                }

                if (expr.tuple) 
                {
                    // tuple stream is carrying ancestry information - keep this
                    resultSequence = tupleBindings;
                } 
                else 
                {
                    resultSequence = JsonataArray.CreateSequence();
                    for (int ii = 0; ii < tupleBindings.Count; ++ii) 
                    {
                        JObject tuple = (JObject)tupleBindings.ChildrenTokens[ii];
                        resultSequence.Add(tuple.Properties["@"]);
                    }
                }
            }

            if (expr.keepSingletonArray)
            {
                //TODO: fix magic based on jsonata-js code
                // If we only got an ArrayList, convert it so we can set the keepSingleton flag
                if (resultSequence is not JsonataArray resultSequenceAsJsonataArray)
                {
                    resultSequenceAsJsonataArray = new JsonataArray(resultSequence!.ChildrenTokens);
                    resultSequence = resultSequenceAsJsonataArray;
                }

                // if the array is explicitly constructed in the expression and marked to promote singleton sequences to array
                if (resultSequenceAsJsonataArray.cons && !resultSequenceAsJsonataArray.sequence)
                {
                    resultSequenceAsJsonataArray = JsonataArray.CreateSequence(resultSequence);
                    resultSequence = resultSequenceAsJsonataArray;
                }
                resultSequenceAsJsonataArray.keepSingleton = true;
            }

            if (expr.group != null)
            {
                JArray groupInput;
                if (isTupleStream)
                {
                    if (tupleBindings == null)
                    {
                        throw new Exception("Should not happen");
                    }
                    groupInput = tupleBindings;
                }
                else
                {
                    groupInput = resultSequence;
                }
                JToken groupResult = JsonataQ.evaluateGroupExpression(expr.group, groupInput, environment);
                return groupResult;
            }

            return resultSequence;
        }

        /**
         * Evaluate a step within a path
        * @param {Object} expr - JSONata expression
        * @param {Object} input - Input data to evaluate against
        * @param {Object} environment - Environment
        * @param {boolean} lastStep - flag the last step in a path
        * @returns {*} Evaluated input data
        */
        private static JArray evaluateStep(Symbol expr, JToken input, EvaluationEnvironment environment, bool lastStep)
        {
            if (expr.type == SymbolType.sort)
            {
                JArray sortResult = JsonataQ.evaluateSortExpression(expr, input, environment);
                if (expr.stages != null)
                {
                    sortResult = JsonataQ.evaluateStages(expr.stages, sortResult, environment);
                }
                return sortResult;
            }

            JArray result = JsonataArray.CreateSequence();

            JArray arrayInput = (JArray)input;
            foreach (JToken child in arrayInput.ChildrenTokens)
            {
                JToken res = JsonataQ.evaluate(expr, child, environment);
                if (expr.stages != null)
                {
                    foreach (Symbol stage in expr.stages)
                    {
                        res = JsonataQ.evaluateFilter(stage.expr!, res, environment);
                    }
                }
                if (res.Type != JTokenType.Undefined)
                {
                    result.Add(res);
                }
            }

            JArray resultSequence;
            if (lastStep 
                && result.Count == 1 
                && (result.ChildrenTokens[0] is JArray childArray) 
                && ((childArray is not JsonataArray childJsonataArray) || !childJsonataArray.sequence)
            )
            {
                resultSequence = childArray;
            }
            else
            {
                // flatten the sequence
                resultSequence = JsonataArray.CreateSequence();
                foreach (JToken res in result.ChildrenTokens)
                {
                    if (!(res is JArray) || ((res is JsonataArray jsonataArray) && jsonataArray.cons))
                    {
                        // it's not an array - just push into the result sequence
                        resultSequence.Add(res);
                    }
                    else
                    {
                        // res is a sequence - flatten it into the parent sequence
                        resultSequence.AddAll(((JArray)res).ChildrenTokens);
                    }
                }
            }

            return resultSequence;
        }

        private static JArray evaluateStages(List<Symbol> stages, JArray input, EvaluationEnvironment environment)
        {
            JArray result = input;
            foreach (Symbol stage in stages) 
            {
                switch(stage.type) 
                {
                case SymbolType.filter:
                    result = JsonataQ.evaluateFilter(stage.expr!, result, environment);
                    break;
                case SymbolType.index:
                    for (int ee = 0; ee < result.Count; ++ee)
                    {
                        JObject tuple = (JObject)result.ChildrenTokens[ee];
                        tuple.Set((string)stage.value!, new JValue(ee));
                    }
                    break;
                }
            }
            return result;
        }

        private static EvaluationEnvironment createFrameFromTuple(EvaluationEnvironment environment, JObject tuple)
        {
            EvaluationEnvironment frame = EvaluationEnvironment.CreateNestedEnvironment(environment);
            foreach (KeyValuePair<string, JToken> proprety in tuple.Properties)
            {
                frame.BindValue(proprety.Key, proprety.Value);
            }
            return frame;
        }

        /**
         * Evaluate a step within a path
        * @param {Object} expr - JSONata expression
        * @param {Object} input - Input data to evaluate against
        * @param {Object} tupleBindings - The tuple stream
        * @param {Object} environment - Environment
        * @returns {*} Evaluated input data
        */
        private static JArray evaluateTupleStep(Symbol expr, JArray input, JArray? tupleBindings, EvaluationEnvironment environment) 
        {
            JArray result;
            if (expr.type == SymbolType.sort) 
            {
                if (tupleBindings != null) 
                {
                    result = JsonataQ.evaluateSortExpression(expr, tupleBindings, environment);
                } 
                else 
                {
                    JArray sorted = JsonataQ.evaluateSortExpression(expr, input, environment);
                    result = JsonataArray.CreateSequence();
                    ((JsonataArray)result).tupleStream = true;
                    for (int ss = 0; ss < sorted.Count; ++ss) 
                    {
                        JObject tuple = new JObject();
                        tuple.Add("@", sorted.ChildrenTokens[ss]);
                        tuple.Add((string)expr.index!, new JValue(ss)); //TODO: hope it's string, not int
                        result.Add(tuple);
                    }
                }
                if (expr.stages != null) 
                {
                    result = JsonataQ.evaluateStages(expr.stages, result, environment);
                }
                return result;
            }

            result = JsonataArray.CreateSequence();
            ((JsonataArray)result).tupleStream = true;
            if (tupleBindings == null) 
            {
                tupleBindings = new JArray();
                tupleBindings.AddAll(input.ChildrenTokens.Select(t => {
                    JObject tuple = new JObject();
                    tuple.Add("@", t);
                    return tuple;
                }));
            }

            foreach (JObject binding in tupleBindings.ChildrenTokens) 
            {
                EvaluationEnvironment stepEnv = createFrameFromTuple(environment, binding);
                JToken _res = JsonataQ.evaluate(expr, binding.Properties["@"], stepEnv);
                // _res is the binding sequence for the output tuple stream
                if (_res.Type != JTokenType.Undefined)
                { 
                    if (_res is not JArray res)
                    {
                        res = new JArray();
                        res.Add(_res);
                    }
                    for (int bb = 0; bb < res.Count; ++bb)
                    {
                        JObject tuple = new JObject();
                        foreach (KeyValuePair<string, JToken> property in binding.Properties)
                        {
                            tuple.Add(property.Key, property.Value);
                        }
                        if ((res is JsonataArray resArray) && resArray.tupleStream) 
                        {
                            JObject sourceChild = (JObject)resArray.ChildrenTokens[bb];
                            foreach (KeyValuePair<string, JToken> property in sourceChild.Properties)
                            {
                                tuple.Set(property.Key, property.Value);
                            }
                        } 
                        else 
                        {
                            if (expr.focus != null) 
                            {
                                tuple.Set(expr.focus, res.ChildrenTokens[bb]);
                                tuple.Set("@", binding.Properties["@"]);
                            } 
                            else 
                            {
                                tuple.Set("@", res.ChildrenTokens[bb]);
                            }
                            if (expr.index != null) //TODO: hope it's string, not int
                            {
                                tuple.Set((string)expr.index, new JValue(bb));
                            }
                            if (expr.ancestor != null) 
                            {
                                tuple.Set(expr.ancestor.label!, binding.Properties["@"]);
                            }
                        }
                        result.Add(tuple);
                    }
                }
            }

            if (expr.stages != null) 
            {
                result = JsonataQ.evaluateStages(expr.stages, result, environment);
            }

            return result;
        }

        /**
         * Apply filter predicate to input data
        * @param {Object} predicate - filter expression
        * @param {Object} input - Input data to apply predicates against
        * @param {Object} environment - Environment
        * @returns {*} Result after applying predicates
        */
        private static JArray evaluateFilter(Symbol predicate, JToken input, EvaluationEnvironment environment)
        {
            JArray results = JsonataArray.CreateSequence();
            bool tupleStream;
            if ((input is JsonataArray inputJsonataArray) && inputJsonataArray.tupleStream)
            {
                ((JsonataArray)results).tupleStream = true;
                tupleStream = true;
            }
            else
            {
                tupleStream = false;
            }

            if (input is not JArray inputArray)
            {
                inputArray = JsonataArray.CreateSequence(input);
                input = inputArray;
            }
            if (predicate.type == SymbolType.number) 
            {
                int index = (int)(long)predicate.value!;  // round it down - was Math.floor //TODO: add Math.Floor if not int
                if (index < 0) 
                {
                    // count in from end of array
                    index = inputArray.Count + index;
                }
                JToken item = index < inputArray.Count ? inputArray.ChildrenTokens[index] : JsonataQ.UNDEFINED;
                if (item.Type != JTokenType.Undefined) 
                {
                    if (item is JArray) 
                    {
                        results = (JArray)item;
                    } 
                    else 
                    {
                        results.Add(item);
                    }
                }
            } 
            else 
            {
                for (int index = 0; index < inputArray.Count; ++index) 
                {
                    JToken item = inputArray.ChildrenTokens[index];
                    JToken context = item;
                    EvaluationEnvironment env = environment;
                    if (tupleStream) 
                    {
                        JObject itemObj = (JObject)item;
                        context = itemObj.Properties["@"];
                        env = JsonataQ.createFrameFromTuple(environment, itemObj);
                    }
                    JToken res = JsonataQ.evaluate(predicate, context, env);
                    if (Utils.isNumeric(res)) 
                    {
                        JArray resArray = new JArray();
                        resArray.Add(res);
                        res = resArray;
                    }
                    if (Utils.isArrayOfNumbers(res)) 
                    {
                        foreach (JToken ires in ((JArray)res).ChildrenTokens) 
                        {
                            // round it down
                            int ii;
                            switch (ires.Type)
                            {
                            case JTokenType.Integer:
                                ii = (int)(JValue)ires;
                                break;
                            case JTokenType.Float:
                                ii = (int)(double)(JValue)ires; // round it down
                                break;
                            default:
                                throw new Exception("Should not happen");
                            }
                            if (ii < 0) 
                            {
                                // count in from end of array
                                ii = inputArray.Count + ii;
                            }
                            if (ii == index) 
                            {
                                results.Add(item);
                            }
                        }
                    } 
                    else if (Eval.Helpers.Booleanize(res)) // truthy
                    { 
                        results.Add(item);
                    }
                }
            }
            return results;
        }

        /**
         * Evaluate binary expression against input data
        * @param {Object} expr - JSONata expression
        * @param {Object} input - Input data to evaluate against
        * @param {Object} environment - Environment
        * @returns {*} Evaluated input data
        */
        private static JToken evaluateBinary(Symbol _expr, JToken input, EvaluationEnvironment environment)
        {
            Infix expr = (Infix)_expr;
            JToken result;
            JToken lhs = JsonataQ.evaluate(expr.lhs!, input, environment);

            if (expr.value is not string)
            {
                throw new JException($"Bad operator", expr.position);
            }
            string op = (string)expr.value;

            if (op == "and" || op == "or")
            {
                //defer evaluation of RHS to allow short-circuiting
                Func<JToken> evalrhs = () => JsonataQ.evaluate(expr.rhs!, input, environment);

                return JsonataQ.evaluateBooleanExpression(lhs, evalrhs, op);
            }

            JToken rhs = evaluate(expr.rhs!, input, environment); //evalrhs();
            switch (op)
            {
            case "+":
            case "-":
            case "*":
            case "/":
            case "%":
                result = JsonataQ.evaluateNumericExpression(lhs, rhs, op);
                break;
            case "=":
            case "!=":
                result = JsonataQ.evaluateEqualityExpression(lhs, rhs, op);
                break;
            case "<":
            case "<=":
            case ">":
            case ">=":
                result = JsonataQ.evaluateComparisonExpression(lhs, rhs, op);
                break;
            case "&":
                result = JsonataQ.evaluateStringConcat(lhs, rhs);
                break;
            case "..":
                result = JsonataQ.evaluateRangeExpression(lhs, rhs);
                break;
            case "in":
                result = JsonataQ.evaluateIncludesExpression(lhs, rhs);
                break;
            default:
                throw new JException($"Unexpected operator '{op}'", expr.position);
            }

            return result;
        }

        //final public static Object NULL_VALUE = new Object() { public String toString() { return "null"; }};

        /**
         * Evaluate unary expression against input data
        * @param {Object} expr - JSONata expression
        * @param {Object} input - Input data to evaluate against
        * @param {Object} environment - Environment
        * @returns {*} Evaluated input data
        */
        private static JToken evaluateUnary(Symbol expr, JToken input, EvaluationEnvironment environment)
        {
            switch (expr.value)  // Uli was: expr.value - where is value set???
            {
            case "-":
                { 
                    JToken result = JsonataQ.evaluate(expr.expression!, input, environment);
                    switch (result.Type)
                    {
                    case JTokenType.Undefined:
                        return JsonataQ.UNDEFINED;
                    case JTokenType.Integer:
                        return new JValue(-(long)result);
                    case JTokenType.Float:
                        return new JValue(-(double)result);
                    default:
                        throw new JException("D1002", expr.position, expr.value, result);
                    }
                }
            case "[":
                {
                    // array constructor - evaluate each item
                    JArray result = new JArray();
                    foreach (Symbol item in expr.expressions!)
                    {
                        JToken value = JsonataQ.evaluate(item, input, environment);
                        if (value != null)
                        {
                            if (item.value!.Equals("["))
                            {
                                result.Add(value);
                            }
                            else
                            {
                                result = (JArray)BuiltinFunctions.append(result, value);
                            }
                        }
                    }
                    if (expr.consarray)
                    {
                        if (result is not JsonataArray jsonataArray)
                        {
                            jsonataArray = new JsonataArray(result.ChildrenTokens);
                            result = jsonataArray;
                        }
                        jsonataArray.cons = true;
                    }
                    return result;
                }
            case "{":
                // object constructor - apply grouping
                {
                    JToken result = JsonataQ.evaluateGroupExpression(expr, input, environment);
                    return result;
                }

            default:
                throw new Exception("Should not happen? " + expr.value);
            }
        }

        /**
         * Evaluate name object against input data
        * @param {Object} expr - JSONata expression
        * @param {Object} input - Input data to evaluate against
        * @param {Object} environment - Environment
        * @returns {*} Evaluated input data
        */
        private static JToken evaluateName(Symbol expr, JToken input, EvaluationEnvironment environment)
        {
            // lookup the "name" item in the input
            if (expr.value is not string strValue)
            {
                throw new Exception("Should not happen");
            }
            return BuiltinFunctions.lookup(input, strValue);
        }

        /**
         * Evaluate literal against input data
         * @param {Object} expr - JSONata expression
         * @returns {*} Evaluated input data
         */
        private static JToken evaluateLiteral(Symbol expr)
        {
            return JValue.FromObject(expr.value);
        }

        /**
         * Evaluate wildcard against input data
        * @param {Object} expr - JSONata expression
        * @param {Object} input - Input data to evaluate against
        * @returns {*} Evaluated input data
        */
        private static JToken evaluateWildcard(Symbol expr, JToken input)
        {
            JsonataArray results = JsonataArray.CreateSequence();
            if ((input is JsonataArray arrayInput) && arrayInput.outerWrapper && arrayInput.Count > 0)
            {
                input = arrayInput.ChildrenTokens[0];
            }
            if (input is JObject objectInput)   // typeof input === "object") {
            {
                foreach (KeyValuePair<string, JToken> kvp in objectInput.Properties)
                {
                    // Object.keys(input).forEach(Object (key) {
                    JToken value = kvp.Value;
                    if (value is JArray)
                    {
                        JToken flatValue = JsonataQ.flatten(value, null);
                        results = (JsonataArray)BuiltinFunctions.append(results, flatValue);
                    }
                    else
                    {
                        results.Add(value);
                    }
                }
            }
            else if (input is JArray inputArray)
            {
                // Java: need to handle List separately
                foreach (JToken value in inputArray.ChildrenTokens)
                {
                    if (value is JArray)
                    {
                        JToken flatValue = JsonataQ.flatten(value, null);
                        results = (JsonataArray)BuiltinFunctions.append(results, flatValue);
                    }
                    else
                    {
                        results.Add(value);
                    }
                }
            }

            // result = normalizeSequence(results);
            return results;
        }

        /**
         * Returns a flattened array
        * @param {Array} arg - the array to be flatten
        * @param {Array} flattened - carries the flattened array - if not defined, will initialize to []
        * @returns {Array} - the flattened array
        */
        private static JArray flatten(JToken arg, JArray? flattened)
        {
            if (flattened == null)
            {
                flattened = new JArray();
            }
            if (arg is JArray arrayArg)
            {
                foreach (JToken item in arrayArg.ChildrenTokens)
                {
                    JsonataQ.flatten(item, flattened);
                }
            }
            else
            {
                flattened.Add(arg);
            }
            return flattened;
        }

        /**
         * Evaluate descendants against input data
        * @param {Object} expr - JSONata expression
        * @param {Object} input - Input data to evaluate against
        * @returns {*} Evaluated input data
        */
        private static JToken evaluateDescendants(Symbol expr, JToken input)
        {
            JToken result;
            if (input.Type != JTokenType.Undefined)
            {
                // traverse all descendants of this object/array
                JsonataArray resultSequence = JsonataArray.CreateSequence();
                JsonataQ.recurseDescendants(input, resultSequence);
                if (resultSequence.Count == 1)
                {
                    result = resultSequence.ChildrenTokens[0];
                }
                else
                {
                    result = resultSequence;
                }
            }
            else
            {
                result = JsonataQ.UNDEFINED;
            }
            return result;
        }

        /**
         * Recurse through descendants
        * @param {Object} input - Input data
        * @param {Object} results - Results
        */
        private static void recurseDescendants(JToken input, JArray results)
        {
            // this is the equivalent of //* in XPath
            if (input is not JArray)
            {
                results.Add(input);
            }
            if (input is JArray inputArray)
            {
                foreach (JToken member in inputArray.ChildrenTokens)
                {
                    JsonataQ.recurseDescendants(member, results);
                }
            }
            else if (input is JObject objectInput)
            {
                //Object.keys(input).forEach(Object (key) {
                foreach (JToken value in objectInput.Properties.Values)
                {
                    JsonataQ.recurseDescendants(value, results);
                }
            }
        }

        /**
         * Evaluate numeric expression against input data
         * @param {Object} lhs - LHS value
         * @param {Object} rhs - RHS value
         * @param {Object} op - opcode
         * @returns {*} Result
         */
        private static JToken evaluateNumericExpression(JToken lhs, JToken rhs, string op)
        {
            if (lhs.Type != JTokenType.Undefined && !Utils.isNumeric(lhs))
            {
                throw new JException("T2001", -1, op, lhs);
            }
            if (rhs.Type != JTokenType.Undefined && !Utils.isNumeric(rhs))
            {
                throw new JException("T2002", -1, op, rhs);
            }

            if (lhs.Type == JTokenType.Undefined || rhs.Type == JTokenType.Undefined)
            {
                // if either side is undefined, the result is undefined
                return JsonataQ.UNDEFINED;
            }

            if (lhs.Type == JTokenType.Undefined || rhs.Type == JTokenType.Undefined)
            {
                return JsonataQ.UNDEFINED;
            }
            else if (lhs.Type == JTokenType.Integer && rhs.Type == JTokenType.Integer)
            {
                if (op == "/")
                {
                    //divide is still in double
                    return evalDoubleOperator((long)lhs, (long)rhs, op);
                }
                else
                {
                    return evalIntOperator((long)lhs, (long)rhs, op);
                }
            }
            else if (lhs.Type == JTokenType.Float && rhs.Type == JTokenType.Float)
            {
                return evalDoubleOperator((double)lhs, (double)rhs, op);
            }
            else if (lhs.Type == JTokenType.Float && rhs.Type == JTokenType.Integer)
            {
                return evalDoubleOperator((double)lhs, (double)(long)rhs, op);
            }
            else if (lhs.Type == JTokenType.Integer && rhs.Type == JTokenType.Float)
            {
                return evalDoubleOperator((double)(long)lhs, (double)rhs, op);
            }
            else if (lhs.Type != JTokenType.Float && lhs.Type != JTokenType.Integer)
            {
                throw new JException("T2001", $"The left side of the {op} operator must evaluate to a number");
            }
            else
            {
                throw new JException("T2002", $"The right side of the {op} operator must evaluate to a number");
            }
        }

        private static JToken evalIntOperator(long lhs, long rhs, string op)
        {
            long result = op switch {
                "+" => lhs + rhs,
                "-" => lhs - rhs,
                "*" => lhs * rhs,
                "/" => lhs / rhs,
                "%" => lhs % rhs,
                _ => throw new ArgumentException($"Unexpected operator '{op}'")
            };
            return new JValue(result);
        }

        private static JToken evalDoubleOperator(double lhs, double rhs, string op)
        {
            double result = op switch {
                "+" => lhs + rhs,
                "-" => lhs - rhs,
                "*" => lhs * rhs,
                "/" => lhs / rhs,
                "%" => lhs % rhs,
                _ => throw new ArgumentException($"Unexpected operator '{op}'")
            };
            long longResult = (long)result;
            if (longResult == result)
            {
                return new JValue(longResult);
            }
            else
            {
                return new JValue(result);
            }
        }


        /**
         * Evaluate equality expression against input data
         * @param {Object} lhs - LHS value
         * @param {Object} rhs - RHS value
         * @param {Object} op - opcode
         * @returns {*} Result
         */
        private static JToken evaluateEqualityExpression(JToken lhs, JToken rhs, string op)
        {
            if (lhs.Type == JTokenType.Undefined || rhs.Type == JTokenType.Undefined)
            {
                // if either side is undefined, the result is false
                return new JValue(false);
            }

            bool result;
            switch (op) 
            {
            case "=":
                result = JToken.DeepEquals(lhs, rhs);
                break;
            case "!=":
                result = !JToken.DeepEquals(lhs, rhs);
                break;
            default:
                throw new Exception("Should not happen " + op);
            }
            return new JValue(result);
        }

        /**
         * Evaluate comparison expression against input data
         * @param {Object} lhs - LHS value
         * @param {Object} rhs - RHS value
         * @param {Object} op - opcode
         * @returns {*} Result
         */
        private static JToken evaluateComparisonExpression(JToken lhs, JToken rhs, string op)
        {
            bool lcomparable = JsonataQ.IsComparable(lhs);
            bool rcomparable = JsonataQ.IsComparable(rhs);

            // if either aa or bb are not comparable (string or numeric) values, then throw an error
            if (!lcomparable || !rcomparable)
            {
                throw new JException("T2010", 0, op, !lcomparable? lhs : rhs);
            }

            // if either side is undefined, the result is undefined
            if (lhs.Type == JTokenType.Undefined || rhs.Type == JTokenType.Undefined)
            {
                return JsonataQ.UNDEFINED;
            }

            if (lhs.Type == JTokenType.Integer && rhs.Type == JTokenType.Float)
            {
                lhs = new JValue((double)(int)lhs);
            }
            else if (rhs.Type == JTokenType.Integer && lhs.Type == JTokenType.Float)
            {
                rhs = new JValue((double)(int)rhs);
            }

            //if aa and bb are not of the same type
            if (lhs.Type != rhs.Type)
            {
                throw new JException(
                    "T2009",
                    0, // location?
                       // stack: (new Error()).stack,
                    lhs,
                    rhs
                );
            }
            if (lhs.Type == JTokenType.String)
            {
                return CompareStrings(op, (string)lhs!, (string)rhs!);
            }
            else if (lhs.Type == JTokenType.Integer)
            {
                return CompareInts(op, (long)lhs, (long)rhs);
            }
            else if (lhs.Type == JTokenType.Float)
            {
                return CompareDoubles(op, (double)lhs, (double)rhs);
            }
            else
            {
                throw new Exception("Should not happen");
            }
        }


		private static bool IsComparable(JToken token)
        {
            return token.Type == JTokenType.Integer
                || token.Type == JTokenType.Float
                || token.Type == JTokenType.String
                || token.Type == JTokenType.Undefined;
        }

        private static JToken CompareDoubles(string op, double lhs, double rhs)
        {
            switch (op)
            {
            case "<":
                return new JValue(lhs < rhs);
            case "<=":
                return new JValue(lhs <= rhs);
            case ">":
                return new JValue(lhs > rhs);
            case ">=":
                return new JValue(lhs >= rhs);
            default:
                throw new Exception("Should not happen");
            }
        }

        private static JToken CompareInts(string op, long lhs, long rhs)
        {
            switch (op)
            {
            case "<":
                return new JValue(lhs < rhs);
            case "<=":
                return new JValue(lhs <= rhs);
            case ">":
                return new JValue(lhs > rhs);
            case ">=":
                return new JValue(lhs >= rhs);
            default:
                throw new Exception("Should not happen");
            }
        }

        private static JToken CompareStrings(string op, string lhs, string rhs)
        {
            switch (op)
            {
            case "<":
                return new JValue(String.CompareOrdinal(lhs, rhs) < 0);
            case "<=":
                return new JValue(String.CompareOrdinal(lhs, rhs) <= 0);
            case ">":
                return new JValue(String.CompareOrdinal(lhs, rhs) > 0);
            case ">=":
                return new JValue(String.CompareOrdinal(lhs, rhs) >= 0);
            default:
                throw new Exception("Should not happen");
            }
        }

        /**
         * Inclusion operator - in
         *
         * @param {Object} lhs - LHS value
         * @param {Object} rhs - RHS value
         * @returns {boolean} - true if lhs is a member of rhs
         */
        private static JToken evaluateIncludesExpression(JToken lhs, JToken rhs)
        {
            if (lhs.Type == JTokenType.Undefined || rhs.Type == JTokenType.Undefined)
            {
                // if either side is undefined, the result is false
                return new JValue(false);
            }

            if (rhs is not JArray rhsArray)
            {
                rhsArray = new JArray();
                rhsArray.Add(rhs);
            }

            foreach (JToken rhsItem in rhsArray.ChildrenTokens)
            {
                JValue res = (JValue)JsonataQ.evaluateEqualityExpression(lhs, rhsItem, "=");
                if (res.Type == JTokenType.Boolean && (bool)res)
                {
                    return res;
                }
            }

            return new JValue(false);
        }

        /**
         * Evaluate boolean expression against input data
         * @param {Object} lhs - LHS value
         * @param {Function} evalrhs - Object to evaluate RHS value
         * @param {Object} op - opcode
         * @returns {*} Result
         */
        private static JToken evaluateBooleanExpression(JToken lhs, Func<JToken> evalrhs, string op)
        {
            bool lBool = Eval.Helpers.Booleanize(lhs);
            bool result;
            switch (op)
            {
            case "and":
                result = lBool && Eval.Helpers.Booleanize(evalrhs.Invoke());
                break;
            case "or":
                result = lBool || Eval.Helpers.Booleanize(evalrhs.Invoke());
                break;
            default:
                throw new Exception("Should not happen " + op);
            }
            return new JValue(result);
        }

        /**
         * Evaluate string concatenation against input data
         * @param {Object} lhs - LHS value
         * @param {Object} rhs - RHS value
         * @returns {string|*} Concatenated string
         */
        private static JToken evaluateStringConcat(JToken lhs, JToken rhs)
        {
            String result;

            string lstr = "";
            string rstr = "";
            if (lhs.Type != JTokenType.Undefined)
            {
                lstr = (string)(JValue)BuiltinFunctions.@string(lhs, prettify: false);
            }
            if (rhs.Type != JTokenType.Undefined)
            {
                rstr = (string)(JValue)BuiltinFunctions.@string(rhs, prettify: false); ;
            }

            result = lstr + rstr;
            return new JValue(result);
        }

        private sealed class GroupEntry 
        {
            internal JToken data;
            internal int exprIndex;

            public GroupEntry(JToken data, int exprIndex)
            {
                this.data = data;
                this.exprIndex = exprIndex;
            }
        }

        /**
         * Evaluate group expression against input data
         * @param {Object} expr - JSONata expression
         * @param {Object} input - Input data to evaluate against
         * @param {Object} environment - Environment
         * @returns {{}} Evaluated input data
         */
        private static JToken evaluateGroupExpression(Symbol expr, JToken _input, EvaluationEnvironment environment)
        {
            bool reduce = (_input is JsonataArray jsonataArrayInput) && jsonataArrayInput.tupleStream;
            // group the input sequence by "key" expression
            if (_input is not JArray input) 
            {
                input = JsonataArray.CreateSequence(_input);
            }

            // if the array is empty, add an undefined entry to enable literal JSON object to be generated
            if (input.Count == 0) 
            {
                input.Add(JsonataQ.UNDEFINED);
            }

            Dictionary<string, GroupEntry> groups = new();
            foreach (JToken item in input.ChildrenTokens) 
            {
                EvaluationEnvironment env = reduce ? JsonataQ.createFrameFromTuple(environment, (JObject)item) : environment;
                for (int pairIndex = 0; pairIndex < expr.lhsObject!.Count; ++pairIndex) 
                {
                    Symbol[] pair = expr.lhsObject[pairIndex];
                    JToken key = JsonataQ.evaluate(pair[0], reduce ? ((JObject)item).Properties["@"] : item, env);
                    // key has to be a string
                    switch (key.Type)
                    {
                    case JTokenType.Undefined:
                        //just skip
                        break;
                    case JTokenType.String:
                        {
                            string keyStr = (string)(JValue)key;
                            if (groups.TryGetValue(keyStr, out GroupEntry? existingEntry))
                            {
                                // a value already exists in this slot
                                if (existingEntry.exprIndex != pairIndex)
                                {
                                    // this key has been generated by another expression in this group
                                    // when multiple key expressions evaluate to the same key, then error D1009 must be thrown
                                    throw new JException("D1009", expr.position, key);
                                }

                                // append it as an array
                                existingEntry.data = BuiltinFunctions.append(existingEntry.data, item);
                            }
                            else
                            {
                                groups.Add(keyStr, new GroupEntry(item, pairIndex));
                            }
                        }
                        break;
                    default:
                        throw new JException("T1003", expr.position, key);
                    }
                }
            }

            JObject result = new JObject();
            // iterate over the groups to evaluate the "value" expression
            foreach (KeyValuePair<string, GroupEntry> groupProperty in groups)
            { 
                GroupEntry entry = groupProperty.Value;
                JToken context = entry.data;
                EvaluationEnvironment env = environment;
                if (reduce) 
                {
                    JObject tuple = JsonataQ.reduceTupleStream(entry.data);
                    context = tuple.Properties["@"];
                    tuple.Remove("@");
                    env = JsonataQ.createFrameFromTuple(environment, tuple);
                }
                //env.isParallelCall = idx > 0;
                //return [key,  evaluate(expr.lhs[entry.exprIndex][1], context, env)];
                JToken res = JsonataQ.evaluate(expr.lhsObject![entry.exprIndex][1], context, env);
                if (res.Type != JTokenType.Undefined)
                {
                    result.Set(groupProperty.Key, res);
                }
            }
            return result;
        }

        private static JObject reduceTupleStream(JToken _tupleStream) 
        {
            if (_tupleStream is not JArray tupleStream) 
            {
                if (_tupleStream.Type != JTokenType.Object)
                {
                    throw new Exception("Should not happen!");
                }
                return (JObject)_tupleStream;
            }
            JObject result = new JObject();
            if (tupleStream.ChildrenTokens.Count == 0)
            {
                throw new Exception("Should not happen!");
            }
            foreach (KeyValuePair<string, JToken> property in ((JObject)tupleStream.ChildrenTokens[0]).Properties)
            {
                result.Add(property.Key, property.Value);
            }
            for (int ii = 1; ii < tupleStream.ChildrenTokens.Count; ++ii)
            {
                JObject child = (JObject)tupleStream.ChildrenTokens[ii];
                foreach (KeyValuePair<string, JToken> property in child.Properties)
                {
                    if (!result.Properties.TryGetValue(property.Key, out JToken? existingValue))
                    {
                        existingValue = JsonataQ.UNDEFINED;
                    }
                    result.Set(property.Key, BuiltinFunctions.append(existingValue, property.Value));
                }
            }
            return result;
        }

        /**
         * Evaluate range expression against input data
         * @param {Object} lhs - LHS value
         * @param {Object} rhs - RHS value
         * @returns {Array} Resultant array
         */
        private static JToken evaluateRangeExpression(JToken lhs, JToken rhs)
        {
            if (lhs.Type != JTokenType.Undefined && lhs.Type != JTokenType.Integer) 
            {
                throw new JException("T2003", -1, lhs);
            }
            if (rhs.Type != JTokenType.Undefined && rhs.Type != JTokenType.Integer) 
            {
                throw new JException("T2004", -1, rhs);
            }

            if (lhs.Type == JTokenType.Undefined || rhs.Type == JTokenType.Undefined) 
            {
                // if either side is undefined, the result is undefined
                return JsonataQ.UNDEFINED;
            }

            long _lhs = (long)(JValue)lhs;
            long _rhs = (long)(JValue)rhs;

            if (_lhs > _rhs) 
            {
                // if the lhs is greater than the rhs, return undefined
                return JsonataQ.UNDEFINED;
            }

            // limit the size of the array to ten million entries (1e7)
            // this is an implementation defined limit to protect against
            // memory and performance issues.  This value may increase in the future.
            long size = _rhs - _lhs + 1;
            if (size > 1e7) 
            {
                throw new JException("D2014", -1, size);
            }

            JsonataArray result = JsonataArray.CreateSequence();
            for (long item = _lhs; item <= _rhs; ++item)
            {
                result.Add(new JValue(item));
            }

            return result;
        }

        /**
         * Evaluate bind expression against input data
         * @param {Object} expr - JSONata expression
         * @param {Object} input - Input data to evaluate against
         * @param {Object} environment - Environment
         * @returns {*} Evaluated input data
         */
        private static JToken evaluateBindExpression(Symbol expr, JToken input, EvaluationEnvironment environment)
        {
            // The RHS is the expression to evaluate
            // The LHS is the name of the variable to bind to - should be a VARIABLE token (enforced by parser)
            JToken value = JsonataQ.evaluate(expr.rhs!, input, environment);
            environment.BindValue((string)expr.lhs!.value!, value);
            return value;
        }

        /**
         * Evaluate condition against input data
         * @param {Object} expr - JSONata expression
         * @param {Object} input - Input data to evaluate against
         * @param {Object} environment - Environment
         * @returns {*} Evaluated input data
         */
        private static JToken evaluateCondition(Symbol expr, JToken input, EvaluationEnvironment environment)
        {
            JToken result;
            JToken condition = JsonataQ.evaluate(expr.condition!, input, environment);
            if (Eval.Helpers.Booleanize(condition))
            {
                result = JsonataQ.evaluate(expr.then!, input, environment);
            }
            else if (expr.@else != null)
            {
                result = evaluate(expr.@else, input, environment);
            }
            else
            {
                result = JsonataQ.UNDEFINED;
            }
            return result;
        }

        /**
         * Evaluate block against input data
         * @param {Object} expr - JSONata expression
         * @param {Object} input - Input data to evaluate against
         * @param {Object} environment - Environment
         * @returns {*} Evaluated input data
         */
        private static JToken evaluateBlock(Symbol expr, JToken input, EvaluationEnvironment environment)
        {

            // create a new frame to limit the scope of variable assignments
            // TODO, only do this if the post-parse stage has flagged this as required
            EvaluationEnvironment frame = EvaluationEnvironment.CreateNestedEnvironment(environment);
            // invoke each expression in turn
            // only return the result of the last one
            JToken result = JsonataQ.UNDEFINED;
            foreach (Symbol ex in expr.expressions!)
            {
                result = JsonataQ.evaluate(ex, input, frame);
            }
            return result;
        }

        /**
         * Prepare a regex
         * @param {Object} expr - expression containing regex
         * @returns {Function} Higher order Object representing prepared regex
         */
        private static JToken evaluateRegex(Symbol expr)
        {
            Regex re = (Regex)expr.value!;
            return new FunctionTokenRegex(re);
        }

        /**
         * Evaluate variable against input data
         * @param {Object} expr - JSONata expression
         * @param {Object} input - Input data to evaluate against
         * @param {Object} environment - Environment
         * @returns {*} Evaluated input data
         */
        private static JToken evaluateVariable(Symbol expr, JToken input, EvaluationEnvironment environment)
        {
            // lookup the variable value in the environment
            JToken result;
            // if the variable name is empty string, then it refers to context value
            if (expr.value is string strValue && strValue == "")
            {
                // Empty string == "$" !
                result = ((input is JsonataArray arrayInput) && arrayInput.outerWrapper) ? arrayInput.ChildrenTokens[0] : input;
            }
            else
            {
                result = environment.Lookup((string)expr.value!);
            }
            return result;
        }

        /**
         * sort / order-by operator
         * @param {Object} expr - AST for operator
         * @param {Object} input - Input data to evaluate against
         * @param {Object} environment - Environment
         * @returns {*} Ordered sequence
         */
        private static JArray evaluateSortExpression(Symbol expr, JToken input, EvaluationEnvironment environment)
        {
            // evaluate the lhs, then sort the results in order according to rhs expression
            JArray lhs = (JArray)input;
            bool isTupleSort = (input is JsonataArray jsonataInput) && jsonataInput.tupleStream;

            // sort the lhs array
            // use comparator function
            
            int comparator(JToken a, JToken b)
            { 
                // expr.terms is an array of order-by in priority order
                int comp = 0;
                for (int index = 0; comp == 0 && index < expr.terms!.Count; ++index) 
                {
                    Symbol term = expr.terms[index];
                    //evaluate the sort term in the context of a
                    JToken context = a;
                    EvaluationEnvironment env = environment;
                    if (isTupleSort) 
                    {
                        JObject aObj = (JObject)a;
                        context = aObj.Properties["@"];
                        env = JsonataQ.createFrameFromTuple(environment, aObj);
                    }
                    JToken aa = JsonataQ.evaluate(term.expression!, context, env);

                    //evaluate the sort term in the context of b
                    context = b;
                    env = environment;
                    if (isTupleSort) 
                    {
                        JObject bObj = (JObject)b;
                        context = bObj.Properties["@"];
                        env = JsonataQ.createFrameFromTuple(environment, bObj);
                    }
                    JToken bb = JsonataQ.evaluate(term.expression!, context, env);
 
                    // type checks
                    // undefined should be last in sort order
                    if (aa.Type == JTokenType.Undefined) 
                    {
                        // swap them, unless btype is also undefined
                        comp = (bb.Type == JTokenType.Undefined) ? 0 : 1;
                        continue;
                    }
                    if (bb.Type == JTokenType.Undefined) 
                    {
                        comp = -1;
                        continue;
                    }
 
                    // if aa or bb are not string or numeric values, then throw an error
                    if (aa.Type != JTokenType.Integer && aa.Type != JTokenType.Float && aa.Type != JTokenType.String)
                    {
                        throw new JException("T2008", expr.position, aa);
                    }

                    JValue aaValue = (JValue)aa;
                    JValue bbValue = (JValue)bb;

                    if (aaValue.Type == JTokenType.String && bbValue.Type == JTokenType.String)
                    {
                        string aaStr = (string)aaValue;
                        string bbStr = (string)bbValue;
                        comp = StringComparer.Ordinal.Compare(aaStr, bbStr);
                        if (comp == 0)
                        {
                            // both the same - move on to next term
                            continue;
                        }
                    }
                    else if (aaValue.Type == JTokenType.String || bbValue.Type == JTokenType.String)
                    {
                        //if aa and bb are not of the same type
                        throw new JException("T2007", expr.position, aa, bb);
                    }
                    else if (aaValue.Type == JTokenType.Integer && bbValue.Type == JTokenType.Integer)
                    {
                        long aaLong = (long)aaValue;
                        long bbLong = (long)bbValue;
                        comp = aaLong.CompareTo(bbLong);
                        if (comp == 0)
                        {
                            // both the same - move on to next term
                            continue;
                        }
                    }
                    else
                    {
                        double aaDouble = aaValue.Type == JTokenType.Float? (double)aaValue : (double)(long)aaValue;
                        double bbDouble = bbValue.Type == JTokenType.Float ? (double)bbValue : (double)(long)bbValue;
                        comp = aaDouble.CompareTo(bbDouble);
                        if (comp == 0)
                        {
                            // both the same - move on to next term
                            continue;
                        }
                    }
                    if (term.descending) 
                    {
                        comp = -comp;
                    }
                }
                // only swap a & b if comp equals 1
                // return comp == 1;
                return comp;
            }

            // var focus = {
            //     environment: environment,
            //     input: input
            // };
            // // the `focus` is passed in as the `this` for the invoked function
            // result = await fn.sort.apply(focus, [lhs, comparator]);
            //
            // but `this` is not being used in builtin sort ..

            //TODO: think of Undefined (
            JArray result = BuiltinFunctions.sort_internal(lhs, comparator);
            return result;        
        }

        /**
         * create a transformer function
         * @param {Object} expr - AST for operator
         * @param {Object} input - Input data to evaluate against
         * @param {Object} environment - Environment
         * @returns {*} tranformer function
         */
        private static JToken evaluateTransformExpression(Symbol expr, JToken input, EvaluationEnvironment environment)
        {
            // create a function to implement the transform definition
            // var transformer = async function (obj) { // signature <(oa):o>
            // ... 
            // }
            // return defineFunction(transformer, '<(oa):o>');
            return new FunctionTokenTransformation(expr.pattern!, expr.update!, expr.delete, environment);
        }

        private static readonly Symbol s_chainAST = BuildChainAst();

        private static Symbol BuildChainAst()
        { 
            Symbol chainAST = new Parser().parse("function($f, $g) { function($x){ $g($f($x)) } }");
            return chainAST;
        }

        /**
         * Apply the Object on the RHS using the sequence on the LHS as the first argument
         * @param {Object} expr - JSONata expression
         * @param {Object} input - Input data to evaluate against
         * @param {Object} environment - Environment
         * @returns {*} Evaluated input data
         */
        private static JToken evaluateApplyExpression(Symbol expr, JToken input, EvaluationEnvironment environment)
        {
            JToken lhs = JsonataQ.evaluate(expr.lhs!, input, environment);

            JToken result;
            if (expr.rhs!.type == SymbolType.function) 
            {
                // this is a function _invocation_; invoke it with lhs expression as the first argument
                result = JsonataQ.evaluateFunction(expr.rhs, input, environment, lhs);
            } 
            else 
            {
                JToken func = JsonataQ.evaluate(expr.rhs, input, environment);
                if (func is not FunctionToken function)
                {
                    throw new JException("T2006", expr.position, func);
                }

                if (lhs is FunctionToken)
                {
                    // this is function chaining (func1 ~> func2)
                    // λ($f, $g) { λ($x){ $g($f($x)) } }
                    JToken chain = JsonataQ.evaluate(s_chainAST, JValue.CreateNull(), environment);
                    if (chain is not FunctionToken chainFunction)
                    {
                        throw new Exception("Should not happen!");
                    }
                    List<JToken> args = new List<JToken>() { lhs, func };
                    result = JsonataQ.apply(chainFunction, args, JValue.CreateNull(), environment);
                } 
                else 
                {
                    List<JToken> args = new List<JToken>() { lhs };
                    result = JsonataQ.apply(function, args, JValue.CreateNull(), environment);
                }
            }
            return result;
        }

        /*
        bool isFunctionLike(Object o) {
            return Utils.isFunction(o) || Functions.isLambda(o) || (o is  Pattern);
        }
        */

        /**
         * Evaluate Object against input data
         * @param {Object} expr - JSONata expression
         * @param {Object} input - Input data to evaluate against
         * @param {Object} environment - Environment
         * @returns {*} Evaluated input data
         */
        private static JToken evaluateFunction(Symbol expr, JToken input, EvaluationEnvironment environment, JToken? applytoContext)
        {
            // create the procedure
            // can"t assume that expr.procedure is a lambda type directly
            // could be an expression that evaluates to a Object (e.g. variable reference, parens expr etc.
            // evaluate it generically first, then check that it is a function.  Throw error if not.
            JToken proc = JsonataQ.evaluate(expr.procedure!, input, environment);
 
            if (proc.Type == JTokenType.Undefined 
                && expr.procedure!.type == SymbolType.path 
                && environment.Lookup((string)(expr.procedure.steps![0].value!)).Type != JTokenType.Undefined
            ) 
            {
                // help the user out here if they simply forgot the leading $
                throw new JException("T1005", expr.position, expr.procedure.steps[0].value);
            }

            List<JToken> evaluatedArgs = new();
            if (applytoContext != null) 
            {
                evaluatedArgs.Add(applytoContext);
            }
            // eager evaluation - evaluate the arguments
            foreach (Symbol argSymbol in expr.arguments!) 
            {
                JToken arg = JsonataQ.evaluate(argSymbol, input, environment);
                if (arg is FunctionToken functionArg)
                {
                    // wrap this in a closure
                    FunctionTokenClosure closure = new FunctionTokenClosure(functionArg, environment);
                    evaluatedArgs.Add(closure);
                } 
                else 
                {
                    evaluatedArgs.Add(arg);
                }
            }
            
            // apply the procedure
            string procName = expr.procedure!.type == SymbolType.path ? (string)expr.procedure.steps![0].value! : (string)expr.procedure.value!;

            try 
            {
                //if (typeof proc === 'object')
                //{
                //    proc.token = procName;
                //    proc.position = expr.position;
                //}

                if (proc is not FunctionToken procFunction)
                {
                    throw new JException("T1006", expr.position, procName);
                }

                JToken result = JsonataQ.apply(procFunction, evaluatedArgs, input, environment);
                return result;
            } 
            catch (JException jex) 
            {
                throw new JException(jex, jex.error, jex.location >= 0? jex.location : expr.position, jex.getCurrent() ?? procName, jex.getExpected());
            } 
        }

        /**
         * Apply procedure or function
         * @param {Object} proc - Procedure
         * @param {Array} args - Arguments
         * @param {Object} input - input
         * @param {Object} environment - environment
         * @returns {*} Result of procedure
         */
        internal static JToken apply(FunctionToken proc, List<JToken> args, JToken input, EvaluationEnvironment environment) 
        {
            JToken result = JsonataQ.applyInner(proc, args, input, environment);
            while (result is FunctionTokenJsonataLambda lambda && lambda.thunk)
            {
                // trampoline loop - this gets invoked as a result of tail-call optimization
                // the Object returned a tail-call thunk
                // unpack it, evaluate its arguments, and apply the tail call
                JToken next = JsonataQ.evaluate(lambda.body.procedure!, lambda.input, lambda.environment);
                if (lambda.body.procedure!.type == SymbolType.variable)
                {
                    //TODO:???
                    //next.token = result.body.procedure.value;
                }
                //next.position = result.body.procedure.position;

                List<JToken> evaluatedArgs = new ();
                foreach (Symbol argSymbol in lambda.body.arguments!) 
                {
                    JToken arg = JsonataQ.evaluate(argSymbol, lambda.input, lambda.environment);
                    evaluatedArgs.Add(arg);
                }
                if (next is not FunctionToken nextFunc)
                {
                    throw new Exception("Should not happen!");
                }
                result = JsonataQ.applyInner(nextFunc, evaluatedArgs, input, environment);
            }
            return result;
        }

        /**
         * Apply procedure or function
         * @param {Object} proc - Procedure
         * @param {Array} args - Arguments
         * @param {Object} input - input
         * @param {Object} environment - environment
         * @returns {*} Result of procedure
         *
         */
        private static JToken applyInner(FunctionToken proc, List<JToken> args, JToken input, EvaluationEnvironment environment) 
        {
            try 
            {
                List<JToken> validatedArgs = args;
                /*
                if (proc.Type != JTokenType.Undefined) 
                {
                    validatedArgs = JsonataQ.validateArguments(proc.signature, args, input);
                }
                */

                JToken result;
                if (proc is FunctionTokenJsonataLambda procLambda) 
                {
                    result = JsonataQ.applyProcedure(procLambda, validatedArgs);
                }
                else if (proc is FunctionTokenCsharp procCsharp) 
                {
                    // var focus = {
                    //    environment: environment,
                    //    input: input
                    // };
                    // the `focus` is passed in as the `this` for the invoked function
                    // result = proc.implementation.apply(focus, validatedArgs);
                    result = procCsharp.Apply(focus_input: input, focus_environment: environment, args: validatedArgs);
                    // `proc.implementation` might be a generator function
                    // and `result` might be a generator - if so, yield
                    // if (isIterable(result))
                    // {
                    //     result = result.next().value;
                    // }
                    // if (isPromise(result))
                    // {
                    //     result = await result;
                    // }
                }
                else
                {
                    // typically these are functions that are returned by the invocation of plugin functions
                    // the `input` is being passed in as the `this` for the invoked function
                    // this is so that functions that return objects containing functions can chain
                    // e.g. await (await $func())
                    // result = proc.apply(input, validatedArgs);

                    // handling special case of Javascript:
                    // when calling a function with fn.apply(ctx, args) and args = [undefined]
                    // Javascript will convert to undefined (without array)
                    // if (validatedArgs is List && ((List)validatedArgs).size()==1 && ((List)validatedArgs).get(0)==null) {
                    //     //validatedArgs = null;
                    //}
                    result = proc.Apply(focus_input: input, focus_environment: null, args: validatedArgs);
                    //  if (isPromise(result)) {
                    //      result =  result;
                    //  }
                }
                return result;
            } 
            catch (JException err) 
            {
                //  if(proc) {
                //      if (typeof err.token == "undefined" && typeof proc.token !== "undefined") {
                //          err.token = proc.token;
                //      }
                //      err.position = proc.position;
                //  }
                throw new JException(err, err.error, err.getLocation(), err.getCurrent(), err.getExpected());
            }
        }

        /**
        * Evaluate lambda against input data
        * @param {Object} expr - JSONata expression
        * @param {Object} input - Input data to evaluate against
        * @param {Object} environment - Environment
        * @returns {{lambda: boolean, input: *, environment: *, arguments: *, body: *}} Evaluated input data
        */
        private static JToken evaluateLambda(Symbol expr, JToken input, EvaluationEnvironment environment)
        {
            /*
            // make a function (closure)
            var procedure = {
                _jsonata_lambda: true,
                input: input,
                environment: environment,
                arguments: expr.arguments,
                signature: expr.signature,
                body: expr.body
            };
            if(expr.thunk === true) {
                procedure.thunk = true;
            }
            procedure.apply = async function(self, args) {
                return await apply(procedure, args, input, !!self ? self.environment : environment);
            };
            return procedure;
            */
            return new FunctionTokenJsonataLambda(expr, input, environment);
        }

        /**
         * Evaluate partial application
         * @param {Object} expr - JSONata expression
         * @param {Object} input - Input data to evaluate against
         * @param {Object} environment - Environment
         * @returns {*} Evaluated input data
         */
        private static JToken evaluatePartialApplication(Symbol expr, JToken input, EvaluationEnvironment environment)
        {
            // partially apply a function
            // evaluate the arguments
            List<JToken?> evaluatedArgsOrPlaceholders = new ();
            foreach (Symbol arg in expr.arguments!) 
            {
                if (arg.type == SymbolType.@operator && (string)arg.value! == "?") 
                {
                    evaluatedArgsOrPlaceholders.Add(null);
                } 
                else 
                {
                    evaluatedArgsOrPlaceholders.Add(JsonataQ.evaluate(arg, input, environment));
                }
            }
            // lookup the procedure
            JToken proc = JsonataQ.evaluate(expr.procedure!, input, environment);
            if (proc.Type == JTokenType.Undefined
                && expr.procedure!.type == SymbolType.path
                && environment.Lookup((string)(expr.procedure.steps![0].value!)).Type != JTokenType.Undefined
            )
            {
                // help the user out here if they simply forgot the leading $
                throw new JException("T1007", expr.position, expr.procedure.steps[0].value);
            }

            JToken result;
            if (proc is FunctionTokenJsonataLambda procLambda)
            {
                result = JsonataQ.partialApplyProcedure(procLambda, evaluatedArgsOrPlaceholders);
            }
            else if (proc is FunctionTokenCsharp procCsharp)
            {
                result = JsonataQ.partialApplyNativeFunction(procCsharp, evaluatedArgsOrPlaceholders);
            }
            else if (proc is FunctionToken procFunc)
            {
                result = JsonataQ.partialApplyNativeFunction(procFunc, evaluatedArgsOrPlaceholders);
            }
            else
            {
                throw new JException("T1008", expr.position, expr.procedure!.type == SymbolType.path ? expr.procedure.steps![0].value : expr.procedure.value);
            }
            return result;
        }

        /**
         * Validate the arguments against the signature validator (if it exists)
         * @param {Function} signature - validator function
         * @param {Array} args - Object arguments
         * @param {*} context - context value
         * @returns {Array} - validated arguments
         *
        private static List<JToken> validateArguments(Object signature, Object args, Object context) 
        {
           var validatedArgs = args;
           if (Utils.isFunction(signature)) {
               validatedArgs = ((JFunction)signature).validate(args, context);
           } else if (Functions.isLambda(signature)) {
               Signature sig = ((Signature) ((Symbol)signature).signature);
               if (sig != null)
                   validatedArgs = sig.validate(args, context);
           }
           return validatedArgs;
       }
        */

        /**
         * Apply procedure
         * @param {Object} proc - Procedure
         * @param {Array} args - Arguments
         * @returns {*} Result of procedure
         **/
        private static JToken applyProcedure(FunctionTokenJsonataLambda proc, List<JToken> args) 
        {
            EvaluationEnvironment env = EvaluationEnvironment.CreateNestedEnvironment(proc.environment);
            for (int index = 0; index < proc.arguments.Count; ++index)
            {
                Symbol param = proc.arguments[index];
                JToken argValue = index < args.Count? args[index] : JsonataQ.UNDEFINED;
                env.BindValue((string)param.value!, argValue);
            }
            JToken result;
            /* TODO?
            if (proc.body.type == SymbolType.function)
            {
                // this is a lambda that wraps a native function - generated by partially evaluating a native
                result = JsonataQ.applyNativeFunction(proc.body, env);
            }
            else
            */
            {
                result = JsonataQ.evaluate(proc.body, proc.input, env);
            }
            return result;
        }
        

        /**
         * Partially apply procedure
         * @param {Object} proc - Procedure
         * @param {Array} args - Arguments
         * @returns {{lambda: boolean, input: *, environment: {bind, lookup}, arguments: Array, body: *}} Result of partially applied procedure
         *
         */

        private static JToken partialApplyProcedure(FunctionTokenJsonataLambda proc, List<JToken?> argsOrPlaceholders) 
        {
            // create a closure, bind the supplied parameters and return a function that takes the remaining (?) parameters
            EvaluationEnvironment env = EvaluationEnvironment.CreateNestedEnvironment(proc.environment);
            List<Symbol> unboundArgs = new();
            for (int index = 0; index < proc.arguments.Count; ++ index)
            {
                Symbol param = proc.arguments[index];
                JToken? arg = argsOrPlaceholders[index];
                if (arg == null)
                {
                    unboundArgs.Add(param);
                }
                else
                {
                    env.BindValue((string)param.value!, arg);
                }
            }

            JToken procedure = new FunctionTokenJsonataLambda(input: proc.input, environment: env, arguments: unboundArgs, body: proc.body);
            return procedure;
        }
        

        /**
         * Partially apply native function
         * @param {Function} native - Native function
         * @param {Array} args - Arguments
         * @returns {{lambda: boolean, input: *, environment: {bind, lookup}, arguments: Array, body: *}} Result of partially applying native function
         *
         */
        private static JToken partialApplyNativeFunction(FunctionToken native, List<JToken?> argsOrPlaceholders) 
        {
            /*
            // create a lambda function that wraps and invokes the native function
            // get the list of declared arguments from the native function
            // this has to be picked out from the toString() value
            var sigArgs = getNativeFunctionArguments(native);
            sigArgs = sigArgs.map(function(sigArg) {
                return '$' + sigArg.trim();
            });
            var body = 'function(' + sigArgs.join(', ') + '){ _ }';

            var bodyAST = parser(body);
            bodyAST.body = native;

            var partial = partialApplyProcedure(bodyAST, args);
            return partial;
            */
            return new FunctionTokenPartial(native, argsOrPlaceholders);
        }

        /**
         * Apply native function
         * @param {Object} proc - Procedure
         * @param {Object} env - Environment
         * @returns {*} Result of applying native function
         *
         *
        Object applyNativeFunction(JFunction proc, Frame env) {
           // Not called in Java - JFunction call directly calls native function
           return null;
        }
        */

        /**
         * Get native Object arguments
         * @param {Function} func - Function
         * @returns {*|Array} Native Object arguments
         *
       List getNativeFunctionArguments(JFunction func) {
           // Not called in Java
           return null;
       }
        */

        /**
         * Creates a Object definition
         * @param {Function} func - Object implementation in Javascript
         * @param {string} signature - JSONata Object signature definition
         * @returns {{implementation: *, signature: *}} Object definition
         *
       static JFunction defineFunction(String func, String signature) {
           return defineFunction(func, signature, func);
       }
       static JFunction defineFunction(String func, String signature, String funcImplMethod) {
           JFunction fn = new JFunction(func, signature, Functions.class, null, funcImplMethod);
           staticFrame.bind(func, fn);
           return fn;
       }

       public static JFunction function(String name, String signature, Class clazz, Object instance, String methodName) {
           return new JFunction(name, signature, clazz, instance, methodName);
       }

       public static<A,B,R> JFunction function(String name, FnVarArgs<R> func, String signature) {
           return new JFunction(func.getJFunctionCallable(), signature);
       }
       public static<A,R> JFunction function(String name, Fn0<R> func, String signature) {
           return new JFunction(func.getJFunctionCallable(), signature);
       }
       public static<A,B,R> JFunction function(String name, Fn1<A,R> func, String signature) {
           return new JFunction(func.getJFunctionCallable(), signature);
       }
       public static<A,B,R> JFunction function(String name, Fn2<A,B,R> func, String signature) {
           return new JFunction(func.getJFunctionCallable(), signature);
       }
       public static<A,B,C,R> JFunction function(String name, Fn3<A,B,C,R> func, String signature) {
           return new JFunction(func.getJFunctionCallable(), signature);
       }
       public static<A,B,C,D,R> JFunction function(String name, Fn4<A,B,C,D,R> func, String signature) {
           return new JFunction(func.getJFunctionCallable(), signature);
       }
       public static<A,B,C,D,E,R> JFunction function(String name, Fn5<A,B,C,D,E,R> func, String signature) {
           return new JFunction(func.getJFunctionCallable(), signature);
       }
       public static<A,B,C,D,E,F,R> JFunction function(String name, Fn6<A,B,C,D,E,F,R> func, String signature) {
           return new JFunction(func.getJFunctionCallable(), signature);
       }
       public static<A,B,C,D,E,F,G,R> JFunction function(String name, Fn7<A,B,C,D,E,F,G,R> func, String signature) {
           return new JFunction(func.getJFunctionCallable(), signature);
       }
       public static<A,B,C,D,E,F,G,H,R> JFunction function(String name, Fn8<A,B,C,D,E,F,G,H,R> func, String signature) {
           return new JFunction(func.getJFunctionCallable(), signature);
       }
        */

        /**
         * parses and evaluates the supplied expression
         * @param {string} expr - expression to evaluate
         * @returns {*} - result of evaluating the expression
         */

        //Object functionEval(String expr, Object focus) {
        // moved to Functions !
        //}

        /**
         * Clones an object
         * @param {Object} arg - object to clone (deep copy)
         * @returns {*} - the cloned object
         */
        //Object functionClone(Object arg) {
        // moved to Functions !
        //}

        /**
         * Create frame
         * @param {Object} enclosingEnvironment - Enclosing environment
         * @returns {{bind: bind, lookup: lookup}} Created frame
         *
       public Frame createFrame() { return createFrame(null); }
       public Frame createFrame(Frame enclosingEnvironment) {
           return new Frame(enclosingEnvironment);

           // The following logic is in class Frame:
           //  var bindings = {};
           //  return {
           //      bind: Object (name, value) {
           //          bindings[name] = value;
           //      },
           //      lookup: Object (name) {
           //          var value;
           //          if(bindings.hasOwnProperty(name)) {
           //              value = bindings[name];
           //          } else if (enclosingEnvironment) {
           //              value = enclosingEnvironment.lookup(name);
           //          }
           //          return value;
           //      },
           //      timestamp: enclosingEnvironment ? enclosingEnvironment.timestamp : null,
           //      async: enclosingEnvironment ? enclosingEnvironment. : false,
           //      isParallelCall: enclosingEnvironment ? enclosingEnvironment.isParallelCall : false,
           //      global: enclosingEnvironment ? enclosingEnvironment.global : {
           //          ancestry: [ null ]
           //      }
           //  };
       }
        */

        /*
        public static interface JLambda {
        }

        public static interface FnVarArgs<R> extends JLambda, Function<List<?>, R> {
            default JFunctionCallable getJFunctionCallable() {
                return (input, args) -> {
                        return apply((List<?>) args);
                };
            }
        }
        public static interface Fn0<R> extends JLambda, Supplier<R> {
            default JFunctionCallable getJFunctionCallable() {
                return (input, args) -> get();
            }
        }
        public static interface Fn1<A,R> extends JLambda, Function<A,R> {
            default JFunctionCallable getJFunctionCallable() {
                return (input, args) -> apply((A) args.get(0));
            }
        }
        public static interface Fn2<A,B,R> extends JLambda, BiFunction<A,B,R> {
            default JFunctionCallable getJFunctionCallable() {
                return (input, args) -> apply((A) args.get(0), (B) args.get(1));
            }
        }
        public static interface Fn3<A,B,C,R> extends JLambda {
            R apply(A a, B b, C c);
            default JFunctionCallable getJFunctionCallable() {
                return (input, args) -> apply((A) args.get(0), (B) args.get(1),
                    (C) args.get(2));
            }
        }
        public static interface Fn4<A,B,C,D,R> extends JLambda {
            R apply(A a, B b, C c, D d);
            default JFunctionCallable getJFunctionCallable() {
                return (input, args) -> apply((A) args.get(0), (B) args.get(1),
                    (C) args.get(2), (D) args.get(3));
            }
        }
        public static interface Fn5<A,B,C,D,E,R> extends JLambda {
            R apply(A a, B b, C c, D d, E e);
            default JFunctionCallable getJFunctionCallable() {
                return (input, args) -> apply((A) args.get(0), (B) args.get(1),
                    (C) args.get(2), (D) args.get(3), (E) args.get(4));
            }
        }
        public static interface Fn6<A,B,C,D,E,F,R> extends JLambda {
            R apply(A a, B b, C c, D d, E e, F f);
            default JFunctionCallable getJFunctionCallable() {
                return (input, args) -> apply((A) args.get(0), (B) args.get(1),
                    (C) args.get(2), (D) args.get(3), (E) args.get(4),
                    (F) args.get(5));
            }
        }
        public static interface Fn7<A,B,C,D,E,F,G,R> extends JLambda {
            R apply(A a, B b, C c, D d, E e, F f, G g);
            default JFunctionCallable getJFunctionCallable() {
                return (input, args) -> apply((A) args.get(0), (B) args.get(1),
                    (C) args.get(2), (D) args.get(3), (E) args.get(4),
                    (F) args.get(5), (G) args.get(6));
            }
        }
        public static interface Fn8<A,B,C,D,E,F,G,H,R> extends JLambda {
            R apply(A a, B b, C c, D d, E e, F f, G g, H h);
            default JFunctionCallable getJFunctionCallable() {
                return (input, args) -> apply((A) args.get(0), (B) args.get(1),
                    (C) args.get(2), (D) args.get(3), (E) args.get(4),
                    (F) args.get(5), (G) args.get(6), (H) args.get(7));
            }
        }
        */

        /**
         * JFunction callable Lambda interface
         *
        public static interface JFunctionCallable {
            Object call(Object input, List args) throws Throwable;
        }

        public static interface JFunctionSignatureValidation {
            Object validate(Object args, Object context);
        }
        */

        /**
         * JFunction definition class
         *
        public static class JFunction implements JFunctionCallable, JFunctionSignatureValidation {
            JFunctionCallable function;
            String functionName;
            Signature signature;
            Method method;
            Object methodInstance;

            public JFunction(JFunctionCallable function, String signature) {
                Jsonata.function = function;
                if (signature!=null)
                    // use classname as default, gets overwritten once the function is registered
                    Jsonata.signature = new Signature(signature, function.getClass().getName());
            }

            public JFunction(String functionName, String signature, Class clz, Object instance, String implMethodName) {
                Jsonata.functionName = functionName;
                Jsonata.signature = new Signature(signature, functionName);
                Jsonata.method = Functions.getFunction(clz, implMethodName);
                Jsonata.methodInstance = instance;
                if (method==null) {
                    System.err.println("Function not implemented: "+functionName+" impl="+implMethodName);
                }
            }

            @Override
            public Object call(Object input, List args) {
                try {
                    if (function!=null) {
                        return function.call(input, args);
                    } else {
                        return Functions.call(methodInstance, method, args);
                    }
                } catch (JException e) {
                    throw e;
                } catch (InvocationTargetException e) {
                    throw new RuntimeException(e.getTargetException());
                } catch (Throwable e) {
                    if (e is  RuntimeException)
                        throw (RuntimeException)e;
                    throw new RuntimeException(e);
                    //throw new JException(e, "T0410", -1, args, functionName);
                }
            }

            @Override
            public Object validate(Object args, Object context) {
                if (signature!=null)
                    return signature.validate(args, context);
                else
                    return args;
            }

            public int getNumberOfArgs() {
                return method != null ? method.getParameterTypes().length : 0;
            }
        }
        */

        /*
         // Function registration
        static void registerFunctions() {
            defineFunction("sum", "<a<n>:n>");
            defineFunction("count", "<a:n>");
            defineFunction("max", "<a<n>:n>");
            defineFunction("min", "<a<n>:n>");
            defineFunction("average", "<a<n>:n>");
            defineFunction("string", "<x-b?:s>");
            defineFunction("substring", "<s-nn?:s>");
            defineFunction("substringBefore", "<s-s:s>");
            defineFunction("substringAfter", "<s-s:s>");
            defineFunction("lowercase", "<s-:s>");
            defineFunction("uppercase", "<s-:s>");
            defineFunction("length", "<s-:n>");
            defineFunction("trim", "<s-:s>");
            defineFunction("pad", "<s-ns?:s>");
            defineFunction("match", "<s-f<s:o>n?:a<o>>");
            defineFunction("contains", "<s-(sf):b>"); // TODO <s-(sf<s:o>):b>
            defineFunction("replace", "<s-(sf)(sf)n?:s>"); // TODO <s-(sf<s:o>)(sf<o:s>)n?:s>
            defineFunction("split", "<s-(sf)n?:a<s>>"); // TODO <s-(sf<s:o>)n?:a<s>>
            defineFunction("join", "<a<s>s?:s>");
            defineFunction("formatNumber", "<n-so?:s>");
            defineFunction("formatBase", "<n-n?:s>");
            defineFunction("formatInteger", "<n-s:s>");
            defineFunction("parseInteger", "<s-s:n>");
            defineFunction("number", "<(nsb)-:n>");
            defineFunction("floor", "<n-:n>");
            defineFunction("ceil", "<n-:n>");
            defineFunction("round", "<n-n?:n>");
            defineFunction("abs", "<n-:n>");
            defineFunction("sqrt", "<n-:n>");
            defineFunction("power", "<n-n:n>");
            defineFunction("random", "<:n>");
            defineFunction("boolean", "<x-:b>", "toBoolean");
            defineFunction("not", "<x-:b>");
            defineFunction("map", "<af>");
            defineFunction("zip", "<a+>");
            defineFunction("filter", "<af>");
            defineFunction("single", "<af?>");
            defineFunction("reduce", "<afj?:j>", "foldLeft"); // TODO <f<jj:j>a<j>j?:j>
            defineFunction("sift", "<o-f?:o>");
            defineFunction("keys", "<x-:a<s>>");
            defineFunction("lookup", "<x-s:x>");
            defineFunction("append", "<xx:a>");
            defineFunction("exists", "<x:b>");
            defineFunction("spread", "<x-:a<o>>");
            defineFunction("merge", "<a<o>:o>");
            defineFunction("reverse", "<a:a>");
            defineFunction("each", "<o-f:a>");
            defineFunction("error", "<s?:x>");
            defineFunction("assert", "<bs?:x>", "assertFn");
            defineFunction("type", "<x:s>");
            defineFunction("sort", "<af?:a>");
            defineFunction("shuffle", "<a:a>");
            defineFunction("distinct", "<x:x>");
            defineFunction("base64encode", "<s-:s>");
            defineFunction("base64decode", "<s-:s>");
            defineFunction("encodeUrlComponent", "<s-:s>");
            defineFunction("encodeUrl", "<s-:s>");
            defineFunction("decodeUrlComponent", "<s-:s>");
            defineFunction("decodeUrl", "<s-:s>");
            defineFunction("eval", "<sx?:x>", "functionEval");
            defineFunction("toMillis", "<s-s?:n>", "dateTimeToMillis");
            defineFunction("fromMillis", "<n-s?s?:s>", "dateTimeFromMillis");
            defineFunction("clone", "<(oa)-:o>", "functionClone");

            defineFunction("now", "<s?s?:s>");
            defineFunction("millis", "<:n>");

            //  environment.bind("now", defineFunction(function(picture, timezone) {
            //      return datetime.fromMillis(timestamp.getTime(), picture, timezone);
            //  }, "<s?s?:s>"));
            //  environment.bind("millis", defineFunction(function() {
            //      return timestamp.getTime();
            //  }, "<:n>"));

        }
        */

        /**
         * lookup a message template from the catalog and substitute the inserts.
         * Populates `err.message` with the substituted message. Leaves `err.message`
         * untouched if code lookup fails.
         * @param {string} err - error code to lookup
         * @returns {undefined} - `err` is modified in place
         */
        private static Exception populateMessage(Exception err)
        {
            //  var template = errorCodes[err.code];
            //  if(typeof template !== "undefined") {
            //      // if there are any handlebars, replace them with the field references
            //      // triple braces - replace with value
            //      // double braces - replace with json stringified value
            //      var message = template.replace(/\{\{\{([^}]+)}}}/g, function() {
            //          return err[arguments[1]];
            //      });
            //      message = message.replace(/\{\{([^}]+)}}/g, function() {
            //          return JSON.stringify(err[arguments[1]]);
            //      });
            //      err.message = message;
            //  }
            // Otherwise retain the original `err.message`
            return err;
        }

        /*
        List<Exception> errors;
        Frame environment;
        Symbol ast;
        long timestamp;
        Object input;

        static {
            staticFrame = new Frame(null);
            registerFunctions();
        }
        */

        private readonly Symbol m_ast;
        //private readonly EvaluationEnvironment m_environment;

        /**
         * JSONata
         * @param {Object} expr - JSONata expression
         * @returns Evaluated expression
         * @throws JException An exception if an error occured.
         */
        public static JsonataQ jsonata(String expression)
        {
            return new JsonataQ(expression);
        }

        /**
         * Internal constructor
         * @param expr
         */
        public JsonataQ(string expr)
        {
            Parser parser = new Parser();
            this.m_ast = parser.parse(expr);
        }

        public JToken evaluate(JToken input, EvaluationEnvironment parentEnvironment)
        {
            // the variable bindings have been passed in - create a frame to hold these
            EvaluationEnvironment environment = EvaluationEnvironment.CreateEvalEnvironment(parentEnvironment);

            // put the input document into the environment as the root object
            environment.BindValue("$", input);

            // if the input is a JSON array, then wrap it in a singleton sequence so it gets treated as a single input
            if (input is JArray /* && !isSequence(input)*/) //it cannot be sequence
            {
                JsonataArray inputWrapper = JsonataArray.CreateSequence(input);
                inputWrapper.outerWrapper = true;
                input = inputWrapper;
            }

            return JsonataQ.evaluate(this.m_ast, input, environment);
        }

        public string FormatAst()
        {
            StringBuilder builder = new StringBuilder();
            this.m_ast.Format(null, builder, 0);
            return builder.ToString();
        }
    }

    public static class JsonataExtensions
    {
        public static JToken evaluate(this JsonataQ query, JToken input)
        {
            return JsonataExtensions.evaluate(query, input, null);
        }

        public static JToken evaluate(this JsonataQ query, JToken input, JObject? bindings)
        {
            EvaluationEnvironment env;
            if (bindings != null)
            {
                env = new EvaluationEnvironment(bindings);
            }
            else
            {
                env = EvaluationEnvironment.DefaultEnvironment;
            }
            return query.evaluate(input, env);
        }

        public static string evaluate(this JsonataQ query, string dataJson, bool indentResult = true)
        {
            JToken data = JToken.Parse(dataJson, ParseSettings.DefaultSettings);
            JToken result = query.evaluate(data);
            return indentResult? result.ToIndentedString() : result.ToFlatString();
        }
    }
}
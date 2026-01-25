using System;
using System.Collections.Generic;
using System.Linq;

namespace Jsonata.Net.Native.New
{ 
    internal sealed class Parser 
    {
        // This parser implements the 'Top down operator precedence' algorithm developed by Vaughan R Pratt; http://dl.acm.org/citation.cfm?id=512931.
        // and builds on the Javascript framework described by Douglas Crockford at http://javascript.crockford.com/tdop/tdop.html
        // and in 'Beautiful Code', edited by Andy Oram and Greg Wilson, Copyright 2007 O'Reilly Media, Inc. 798-0-596-51004-6

        private string source = default!;
        private Tokenizer lexer = default!;

        //var parser = function (source, recover) {
        internal NodeFactoryBase currentNodeFactory { get; private set; } = default!;
        internal Token currentToken { get; private set; } = default!;

        internal void advance() 
        { 
            this.advance(null); 
        }
        
        internal void advance(string? id) 
        { 
            this.advance(id, false); 
        }

        internal void advance(string? id, bool infix) 
        {
            if (id != null && this.currentNodeFactory.id != id) 
            {
                String code;
                if (this.currentNodeFactory == Parser.s_terminalFactoryEnd) 
                {
                    // unexpected end of buffer
                    code = "S0203";
                } 
                else 
                {
                    code = "S0202";
                }
                throw new JException(
                    code,
                    this.currentToken.position,
                    id,
                    this.currentToken.value
                );
            }
            Token? next_token = lexer.next(infix);
            NodeFactoryBase factory;
            if (next_token == null) 
            {
                this.currentNodeFactory = s_terminalFactoryEnd;
                this.currentToken = new Token(SymbolType._end, null, source.Length);
                return;
            }
            this.currentToken = next_token;
            switch (this.currentToken.type) 
            {
            case SymbolType.name:
                factory = Parser.s_terminalFactoryName;
                break;
            case SymbolType.variable:
                factory = Parser.s_terminalFactoryVariable;
                break;
            case SymbolType.@operator:
                if (!Parser.s_nodeFactoryTable.TryGetValue(this.currentToken.value!.ToString()!, out NodeFactoryBase? foundFactory))
                {
                    throw new JException("S0204", this.currentToken.position, this.currentToken.value);
                }
                else
                {
                    factory = foundFactory;
                }
                break;
            case SymbolType.@string:
                factory = Parser.s_terminalFactoryString;
                break;
            case SymbolType.number:
                factory = Parser.s_terminalFactoryNumber;
                break;
            case SymbolType.value:
                factory = Parser.s_terminalFactoryValue;
                break;
            case SymbolType.regex:
                factory = Parser.s_terminalFactoryRegex;
                break;
            default:
                throw new JException("S0205", this.currentToken.position, this.currentToken.value);
            }

            this.currentNodeFactory = factory;
        }

        // Pratt's algorithm
        internal Node expression(int rbp) 
        {
            Node left;
            NodeFactoryBase f = this.currentNodeFactory;
            Token t = this.currentToken;
            this.advance(null, true);
            left = f.nud(this, t);
            while (rbp < this.currentNodeFactory.bp) //was LBP
            {
                f = this.currentNodeFactory;
                t = this.currentToken;
                advance(null, false);
                left = f.led(left, this, t);
            }
            return left;
        }

        // tail call optimization
        // this is invoked by the post parser to analyse lambda functions to see
        // if they make a tail call.  If so, it is replaced by a thunk which will
        // be invoked by the trampoline loop during function application.
        // This enables tail-recursive functions to be written without growing the stack
        private Node tailCallOptimize(Node expr) 
        {
            Node result;
            if (expr.type == SymbolType.function && expr.predicate == null) 
            {
                Node thunk = new Node(SymbolType.lambda, null, expr.position); 
                thunk.thunk = true; 
                thunk.arguments = new(); 
                thunk.body = expr;
                result = thunk;
            } 
            else if (expr.type == SymbolType.condition) 
            {
                ConditionNode conditionExpr = (ConditionNode)expr;
                // analyse both branches
                Node then = this.tailCallOptimize(conditionExpr.then);
                Node? @else;
                if (conditionExpr.@else != null)
                {
                    @else = this.tailCallOptimize(conditionExpr.@else);
                }
                else
                {
                    @else = null;
                }
                result = new ConditionNode(conditionExpr.position, conditionExpr.condition, then, @else);
            } 
            else if (expr.type == SymbolType.block) 
            {
                // only the last expression in the block
                int length = expr.expressions!.Count;
                if (length > 0) 
                {
                    expr.expressions[length - 1] = this.tailCallOptimize(expr.expressions[length - 1]);
                }
                result = expr;
            } 
            else 
            {
                result = expr;
            }
            return result;
        }

        int ancestorLabel = 0;
        int ancestorIndex = 0;
        List<Node> ancestry = new();

        private Node seekParent(Node node, Node slot) 
        {
            switch (node.type) 
            {
            case SymbolType.name:
            case SymbolType.wildcard:
                --slot.level;
                if (slot.level == 0) 
                {
                    if (node.ancestor == null) 
                    {
                        node.ancestor = slot;
                    } 
                    else 
                    {
                        // reuse the existing label
                        this.ancestry[slot.index_int!.Value].slot!.label = node.ancestor.label;
                        node.ancestor = slot;
                    }
                    node.tuple = true;
                }
                break;
            case SymbolType.parent:
                ++slot.level;
                break;
            case SymbolType.block:
                // look in last expression in the block
                if (node.expressions!.Count > 0) 
                {
                    node.tuple = true;
                    slot = this.seekParent(node.expressions[node.expressions.Count - 1], slot);
                }
                break;
            case SymbolType.path:
                {
                    // last step in path
                    PathNode pathNode = (PathNode)node;
                    node.tuple = true;
                    int index = pathNode.steps!.Count - 1;
                    slot = this.seekParent(pathNode.steps[index--], slot);
                    while (slot.level > 0 && index >= 0)
                    {
                        // check previous steps
                        slot = this.seekParent(pathNode.steps[index--], slot);
                    }
                }
                break;
            default:
                // error - can't derive ancestor
                throw new JException("S0217", node.position, node.type);
            }
            return slot;
        }

        private void pushAncestry(Node result, Node? value) 
        {
            if (value == null)
            {
                return; // Added NPE check
            }
            if (value.seekingParent != null || value.type == SymbolType.parent) 
            {
                List<Node> slots = value.seekingParent ?? new();
                if (value.type == SymbolType.parent) 
                {
                    slots.Add(value.slot!);
                }
                if (result.seekingParent == null) 
                {
                    result.seekingParent = slots;
                } 
                else 
                {
                    result.seekingParent.AddRange(slots);
                }
            }
        }

        private void resolveAncestry(PathNode path) 
        {
            int index = path.steps.Count - 1;
            Node laststep = path.steps[index];
            List<Node> slots = laststep.seekingParent ?? new();
            if (laststep.type == SymbolType.parent) 
            {
                slots.Add(laststep.slot!);
            }
            for (int i = 0; i < slots.Count; ++i) 
            {
                Node slot = slots[i];
                index = path.steps.Count - 2;
                while (slot.level > 0) 
                {
                    if (index < 0) 
                    {
                        if (path.seekingParent == null) 
                        {
                            path.seekingParent = new();
                        } 
                        path.seekingParent.Add(slot);
                        break;
                    }
                    // try previous step
                    Node step = path.steps[index--];
                    // multiple contiguous steps that bind the focus should be skipped
                    while (index >= 0 && step.focus != null && path.steps[index].focus != null) 
                    {
                        step = path.steps[index--];
                    }
                    slot = this.seekParent(step, slot);
                }
            }
        }

        // post-parse stage
        // the purpose of this is to add as much semantic value to the parse tree as possible
        // in order to simplify the work of the evaluator.
        // This includes flattening the parts of the AST representing location paths,
        // converting them to arrays of steps which in turn may contain arrays of predicates.
        // following this, nodes containing '.' and '[' should be eliminated from the AST.
        private Node processAST(Node expr) 
        {
            Node result = expr;
            switch (expr.type)
            {
            case SymbolType.binary:
                BinaryNode exprBinary = (BinaryNode)expr;
                switch (exprBinary.value!.ToString())
                {
                case ".":
                    {
                        PathNode resultPath;
                        Node lstep = this.processAST(exprBinary.lhs);
                        if (lstep.type == SymbolType.path)
                        {
                            resultPath = (PathNode)lstep;
                        }
                        else
                        {
                            resultPath = new PathNode(new() { lstep });
                        }
                        if (lstep.type == SymbolType.parent)
                        {
                            resultPath.seekingParent = new() { lstep.slot! };
                        }
                        Node rest = this.processAST(exprBinary.rhs);
                        if (rest.type == SymbolType.function &&
                            rest.procedure!.type == SymbolType.path &&
                            rest.procedure is PathNode procedurePath &&
                            procedurePath.steps.Count == 1 &&
                            procedurePath.steps[0].type == SymbolType.name &&
                            resultPath.steps[^1].type == SymbolType.function
                        )
                        {
                            // next function in chain of functions - will override a thenable
                            resultPath.steps[^1].nextFunction = (Node)procedurePath.steps[0].value!;
                        }
                        if (rest.type == SymbolType.path)
                        {
                            resultPath.steps.AddRange(((PathNode)rest).steps);
                        }
                        else
                        {
                            if (rest.predicate != null)
                            {
                                rest.stages = rest.predicate;
                                rest.predicate = null;
                            }
                            resultPath.steps.Add(rest);
                        }
                        // any steps within a path that are string literals, should be changed to 'name'
                        for (int i = 0; i < resultPath.steps.Count; ++i)
                        {
                            Node step = resultPath.steps[i];
                            if (step.type == SymbolType.number || step.type == SymbolType.value)
                            {
                                // don't allow steps to be numbers or the values true/false/null
                                throw new JException("S0213", step.position, step.value);
                            }
                            if (step.type == SymbolType.@string)
                            {
                                //step.type = SymbolType.name;
                                resultPath.steps[i] = new Node(SymbolType.name, step.value, step.position);
                            }
                        }

                        // any step that signals keeping a singleton array, should be flagged on the path
                        if (resultPath.steps.Any(step => step.keepArray))
                        {
                            resultPath.keepSingletonArray = true;
                        }
                        // if first step is a path constructor, flag it for special handling
                        Node firststep = resultPath.steps[0];
                        if (firststep.type == SymbolType.unary && firststep.value!.Equals("["))
                        {
                            firststep.consarray = true;
                        }
                        // if the last step is an array constructor, flag it so it doesn't flatten
                        Node laststep = resultPath.steps[^1];
                        if (laststep.type == SymbolType.unary && laststep.value!.Equals("["))
                        {
                            laststep.consarray = true;
                        }
                        this.resolveAncestry(resultPath);
                        result = resultPath;
                    }
                    break;
                case "[":
                    {
                        // predicated step
                        // LHS is a step or a predicated step
                        // RHS is the predicate expr
                        result = this.processAST(exprBinary.lhs);
                        Node step = result;
                        SymbolType type = SymbolType.predicate;
                        if (result.type == SymbolType.path)
                        {
                            step = ((PathNode)result).steps[^1];
                            type = SymbolType.stages;
                        }
                        if (step.group != null)
                        {
                            throw new JException("S0209", expr.position);
                        }
                        // if (typeof step[type] === 'undefined') {
                        //     step[type] = [];
                        // }
                        if (type == SymbolType.stages)
                        {
                            if (step.stages == null)
                            {
                                step.stages = new();
                            }
                        }
                        else
                        {
                            if (step.predicate == null)
                            {
                                step.predicate = new();
                            }
                        }

                        Node predicate = this.processAST(exprBinary.rhs);
                        if (predicate.seekingParent != null)
                        {
                            foreach (Node slot in predicate.seekingParent)
                            {
                                if (slot.level == 1) 
                                {
                                    this.seekParent(step, slot);
                                } else 
                                {
                                    --slot.level;
                                }
                            }
                            this.pushAncestry(step, predicate);
                        }
                        Node s = new Node(SymbolType.filter, null, expr.position);
                        s.expr = predicate;

                        // FIXED:
                        // this logic is required in Java to fix
                        // for example test: flattening case 045
                        // otherwise we lose the keepArray flag
                        if (expr.keepArray)
                        {
                            step.keepArray = true;
                        }

                        if (type == SymbolType.stages)
                        {
                            step.stages!.Add(s);
                        }
                        else
                        {
                            step.predicate!.Add(s);
                        }
                        //step[type].push({type: 'filter', expr: predicate, position: expr.position});
                    }
                    break;
                case ":=":
                    {
                        Node lhs = this.processAST(exprBinary.lhs);
                        Node rhs = this.processAST(exprBinary.rhs);
                        result = new BindNode(exprBinary.position, lhs, rhs);
                        this.pushAncestry(result, rhs);
                    }
                    break;
                case "@":
                    {
                        result = this.processAST(exprBinary.lhs);
                        Node step = result;
                        if (result.type == SymbolType.path)
                        {
                            step = ((PathNode)result).steps[^1];
                        }
                        // throw error if there are any predicates defined at this point
                        // at this point the only type of stages can be predicates
                        if (step.stages != null || step.predicate != null)
                        {
                            throw new JException("S0215", exprBinary.position);
                        }
                        // also throw if this is applied after an 'order-by' clause
                        if (step.type == SymbolType.sort)
                        {
                            throw new JException("S0216", exprBinary.position);
                        }
                        if (exprBinary.keepArray)
                        {
                            step.keepArray = true;
                        }
                        step.focus = (string)exprBinary.rhs.value!;
                        step.tuple = true;
                    }
                    break;
                case "#":
                    {
                        result = processAST(exprBinary.lhs);
                        Node step;
                        if (result.type == SymbolType.path)
                        {
                            step = ((PathNode)result).steps[^1];
                        }
                        else
                        {
                            step = result;
                            result = new PathNode(new() { result });
                            if (step.predicate != null)
                            {
                                step.stages = step.predicate;
                                step.predicate = null;
                            }
                        }
                        if (step.stages == null)
                        {
                            step.index_string = (string)exprBinary.rhs.value!; // name of index variable = String
                        }
                        else
                        {
                            Node _res = new Node(SymbolType.index, exprBinary.rhs.value, expr.position);
                            step.stages.Add(_res);
                        }
                        step.tuple = true;
                    }
                    break;
                case "~>":
                    {
                        Node lhs = this.processAST(exprBinary.lhs);
                        Node rhs = this.processAST(exprBinary.rhs);
                        result = new ApplyNode(exprBinary.position, lhs, rhs);
                        result.keepArray = lhs.keepArray || rhs.keepArray;
                    }
                    break;
                default:
                    {
                        Node lhs = this.processAST(exprBinary.lhs);
                        Node rhs = this.processAST(exprBinary.rhs);
                        result = new BinaryNode((string)expr.value!, expr.position, lhs, rhs);
                        this.pushAncestry(result, lhs);
                        this.pushAncestry(result, rhs);
                    }
                    break;
                }
                break; // binary
            case SymbolType._binary_groupby:
                {
                    // group-by
                    // LHS is a step or a predicated step
                    // RHS is the object constructor expr
                    GroupByNode exprGroupby = (GroupByNode)expr;
                    result = this.processAST(exprGroupby.lhs);
                    if (result == null)
                    {
                        throw new Exception("Should not happen?");
                    }
                    if (result.group != null)
                    {
                        throw new JException("S0210", expr.position);
                    }
                    // object constructor - process each pair
                    result.group = new Node(SymbolType._group, null, expr.position);
                    result.group.lhsObject = exprGroupby.rhsObject
                        .Select(pair => Tuple.Create(this.processAST(pair.Item1), this.processAST(pair.Item2)))
                        .ToList();
                }
                break; // _binary_groupby
            case SymbolType._binary_orderby:
                {
                    // order-by
                    // LHS is the array to be ordered
                    // RHS defines the terms
                    OrderbyNode exprOrderby = (OrderbyNode)expr;
                    Node res = this.processAST(exprOrderby.lhs);
                    PathNode resultPath;
                    if (res.type == SymbolType.path)
                    {
                        resultPath = (PathNode)res;
                    }
                    else
                    {
                        resultPath = new PathNode(new() { res });
                    }
                    Node sortStep = new Node(SymbolType.sort, null, expr.position);
                    sortStep.terms = exprOrderby.rhsTerms.Select(terms => {
                        Node expression = this.processAST(terms.expression!);
                        this.pushAncestry(sortStep, expression);
                        Node res_ = new Node(SymbolType._sort_term, null, -1);
                        res_.descending = terms.descending;
                        res_.expression = expression;
                        return res_;
                    }).ToList();
                    resultPath.steps!.Add(sortStep);
                    result = resultPath;
                    this.resolveAncestry(resultPath);
                }
                break; // _tinary_sort

            case SymbolType.unary:
                {
                    result = new Node(SymbolType.unary, expr.value, expr.position);
                    // expr.value might be Character!
                    string exprValue = expr.value!.ToString()!;
                    if (exprValue == "[")
                    {
                        // array constructor - process each item
                        result.expressions = expr.expressions!.Select(item => {
                            Node value = this.processAST(item);
                            this.pushAncestry(result, value);
                            return value;
                        }).ToList();
                    }
                    else if (exprValue == "{")
                    {
                        // object constructor - process each pair
                        //throw new Error("processAST {} unimpl");
                        result.lhsObject = expr.lhsObject!.Select(pair => {
                            Node key = this.processAST(pair.Item1);
                            this.pushAncestry(result, key);
                            Node value = this.processAST(pair.Item2);
                            this.pushAncestry(result, value);
                            return Tuple.Create(key, value);
                        }).ToList();
                    } 
                    else 
                    {
                        // all other unary expressions - just process the expression
                        result.expression = this.processAST(expr.expression!);
                        // if unary minus on a number, then pre-process
                        if (exprValue == "-" && result.expression.type == SymbolType.number)
                        {
                            if (result.expression.value is long longValue)
                            {
                                result = new Node(SymbolType.number, -longValue, result.expression.position);
                            }
                            else if (result.expression.value is double doubleValue)
                            {
                                result = new Node(SymbolType.number, -doubleValue, result.expression.position);
                            }
                            else 
                            {
                                throw new Exception("Should not happen");
                            }
                        }
                        else
                        {
                            this.pushAncestry(result, result.expression);
                        }
                    }
                }
                break; // unary

            case SymbolType.function:
            case SymbolType.@partial:
                {
                    result = new Node(expr.type, expr.value, expr.position);
                    result.name = expr.name;
                    result.arguments = expr.arguments!.Select(arg => {
                        Node argAST = this.processAST(arg);
                        this.pushAncestry(result, argAST);
                        return argAST;
                    }).ToList();
                    result.procedure = processAST(expr.procedure!);
                }
                break;
            case SymbolType.lambda:
                {
                    result = new Node(SymbolType.lambda, null, expr.position);
                    result.arguments = expr.arguments;
                    result.signature = expr.signature;
                    Node body = this.processAST(expr.body!);
                    result.body = this.tailCallOptimize(body);
                }
                break;
            case SymbolType.condition:
                {
                    ConditionNode exprCondition = (ConditionNode)expr;

                    Node condition = this.processAST(exprCondition.condition);
                    Node then = this.processAST(exprCondition.then);
                    Node? @else;
                    if (exprCondition.@else != null)
                    {
                        @else = this.processAST(exprCondition.@else);
                    }
                    else
                    {
                        @else = null;
                    }
                    ConditionNode resultCondition = new ConditionNode(expr.position, condition, then, @else);
                    this.pushAncestry(resultCondition, resultCondition.condition);
                    this.pushAncestry(resultCondition, resultCondition.then);
                    if (resultCondition.@else != null)
                    {
                        this.pushAncestry(resultCondition, resultCondition.@else);
                    }
                    result = resultCondition;
                }
                break;
            case SymbolType.transform:
                {
                    TransformNode exprTransform = (TransformNode)expr;
                    Node pattern = this.processAST(exprTransform.pattern!);
                    Node update = this.processAST(exprTransform.update!);
                    Node? delete;
                    if (exprTransform.delete != null)
                    {
                        delete = this.processAST(exprTransform.delete);
                    }
                    else
                    {
                        delete = null;
                    }
                    result = new TransformNode(expr.position, pattern, update, delete);
                }
                break;
            case SymbolType.block:
                {
                    result = new Node(SymbolType.block, null, expr.position);
                    // array of expressions - process each one
                    result.expressions = expr.expressions!.Select(item => {
                        Node part = this.processAST(item);
                        this.pushAncestry(result, part);
                        if (part.consarray || (part.type == SymbolType.path && ((PathNode)part).steps![0].consarray)) 
                        {
                            result.consarray = true;
                        }
                        return part;
                    }).ToList();
                    // TODO scan the array of expressions to see if any of them assign variables
                    // if so, need to mark the block as one that needs to create a new frame
                }
                break;
            case SymbolType.name:
                {
                    result = new PathNode(new() { expr }) {
                        keepSingletonArray = expr.keepArray
                    };
                }
                break;
            case SymbolType.parent:
                {
                    result = new Node(SymbolType.parent, null, -1);
                    result.slot = new Node(SymbolType._slot, null, -1);
                    result.slot.label = "!" + this.ancestorLabel++;
                    result.slot.level = 1;
                    result.slot.index_int = this.ancestorIndex++;
                    this.ancestry.Add(result);
                }
                break;
            case SymbolType.@string:
            case SymbolType.number:
            case SymbolType.value:
            case SymbolType.wildcard:
            case SymbolType.descendant:
            case SymbolType.variable:
            case SymbolType.regex:
                result = expr;
                break;
            case SymbolType.@operator:
                {
                    // the tokens 'and' and 'or' might have been used as a name rather than an operator
                    if (expr.value!.Equals("and") || expr.value.Equals("or") || expr.value.Equals("in"))
                    {
                        //expr.type = SymbolType.name;
                        //result = this.processAST(expr);

                        Node newExpr = new Node(SymbolType.name, expr.value, expr.position);
                        result = this.processAST(newExpr);
                    }
                    else if (expr.value.Equals("?"))
                    {
                        // partial application
                        result = expr;
                    }
                    else
                    {
                        throw new JException("S0201", expr.position, expr.value);
                    }
                }
                break;
            // case SymbolType.error:
            //     result = expr;
            //     if (expr.lhs != null) 
            //     {
            //         result = this.processAST(expr.lhs);
            //     }
            //     break;
            default:
                {
                    string code = "S0206";
                    if (expr.type == SymbolType._end)
                    {
                        code = "S0207";
                    }
                    throw new JException(code, expr.position, expr.value);
                }
            }
            if (expr.keepArray) 
            {
                result.keepArray = true;
            }
            return result;
        }

        internal Node parse(string jsonata) 
        {
            this.source = jsonata;

            // now invoke the tokenizer and the parser and return the syntax tree
            this.lexer = new Tokenizer(source);
            this.advance();
            // parse the tokens
            Node expr = this.expression(0);
            if (this.currentNodeFactory != Parser.s_terminalFactoryEnd) 
            {
                throw new JException("S0201", this.currentToken.position, this.currentToken.value);
            }

            expr = this.processAST(expr);

            if (expr.type == SymbolType.parent || expr.seekingParent != null) 
            {
                // error - trying to derive ancestor at top level
                throw new JException("S0217", expr.position, expr.type);
            }

            return expr;
        }

        internal static Node Parse(string query)
        {
            Parser parser = new Parser();
            return parser.parse(query);
        }

        private static readonly Dictionary<string, NodeFactoryBase> s_nodeFactoryTable = CreateNodeFactoryTable();
        internal static readonly NodeFactoryBase s_terminalFactoryEnd = new TerminalFactoryTyped(SymbolType._end);
        internal static readonly NodeFactoryBase s_terminalFactoryName = new TerminalFactoryTyped(SymbolType.name);
        internal static readonly NodeFactoryBase s_terminalFactoryVariable = new TerminalFactoryTyped(SymbolType.variable);
        internal static readonly NodeFactoryBase s_terminalFactoryNumber = new TerminalFactoryTyped(SymbolType.number);
        internal static readonly NodeFactoryBase s_terminalFactoryString = new TerminalFactoryTyped(SymbolType.@string);
        internal static readonly NodeFactoryBase s_terminalFactoryValue = new TerminalFactoryTyped(SymbolType.value);
        internal static readonly NodeFactoryBase s_terminalFactoryRegex = new TerminalFactoryTyped(SymbolType.regex);

        private static void register(Dictionary<string, NodeFactoryBase> nodeFactoryTable, NodeFactoryBase t)
        {
            if (nodeFactoryTable.TryGetValue(t.id, out NodeFactoryBase? s))
            {
                throw new Exception("Handle combine?? " + t.id);
            }
            else
            {
                nodeFactoryTable.Add(t.id, t);
            }
        }

        private static Dictionary<string, NodeFactoryBase> CreateNodeFactoryTable() 
        {
            Dictionary<string, NodeFactoryBase> nodeFactoryTable = new();
            register(nodeFactoryTable, new DummyNodeFactory(":"));
            register(nodeFactoryTable, new DummyNodeFactory(";"));
            register(nodeFactoryTable, new DummyNodeFactory(","));
            register(nodeFactoryTable, new DummyNodeFactory(")"));
            register(nodeFactoryTable, new DummyNodeFactory("]"));
            register(nodeFactoryTable, new DummyNodeFactory("}"));
            register(nodeFactoryTable, new DummyNodeFactory("..")); // range operator
            register(nodeFactoryTable, new InfixFactory(".")); // map operator
            register(nodeFactoryTable, new InfixFactory("+")); // numeric addition
            register(nodeFactoryTable, new InfixAndPrefixFactory("-")); // numeric subtraction // unary numeric negation

            register(nodeFactoryTable, new InfixWithTypedNudFactory("*", SymbolType.wildcard)); // field wildcard (single level) // numeric multiplication
            register(nodeFactoryTable, new InfixFactory("/")); // numeric division
            register(nodeFactoryTable, new InfixWithTypedNudFactory("%", SymbolType.parent)); // parent operator // numeric modulus
            register(nodeFactoryTable, new InfixFactory("=")); // equality
            register(nodeFactoryTable, new InfixFactory("<")); // less than
            register(nodeFactoryTable, new InfixFactory(">")); // greater than
            register(nodeFactoryTable, new InfixFactory("!=")); // not equal to
            register(nodeFactoryTable, new InfixFactory("<=")); // less than or equal
            register(nodeFactoryTable, new InfixFactory(">=")); // greater than or equal
            register(nodeFactoryTable, new InfixFactory("&")); // string concatenation

            register(nodeFactoryTable, new InfixWithTypedNudFactory("and", SymbolType.@operator)); // allow as terminal // Boolean AND
            register(nodeFactoryTable, new InfixWithTypedNudFactory("or", SymbolType.@operator)); // allow as terminal // Boolean OR
            register(nodeFactoryTable, new InfixWithTypedNudFactory("in", SymbolType.@operator)); // allow as terminal // is member of array
            // merged Infix: register(new Terminal("and")); // the 'keywords' can also be used as terminals (field names)
            // merged Infix: register(new Terminal("or")); //
            // merged Infix: register(new Terminal("in")); //
            // merged Infix: register(new Prefix("-")); // unary numeric negation
            register(nodeFactoryTable, new InfixFactory("~>")); // function application

            // coalescing operator
            register(nodeFactoryTable, new InfixCoalescingFactory("??", Tokenizer.OPERATORS["??"]));

            register(nodeFactoryTable, new InfixRErrorFactory("(error)", 10));

            // field wildcard (single level)
            // merged with Infix *
            // register(new Prefix("*") {
            //     @Override Symbol nud() {
            //         type = "wildcard";
            //         return this;
            //     }
            // });

            // descendant wildcard (multi-level)

            register(nodeFactoryTable, new PrefixDescendantWindcardFactory("**"));

            // parent operator
            // merged with Infix %
            // register(new Prefix("%") {
            //     @Override Symbol nud() {
            //         type = "parent";
            //         return this;
            //     }
            // });

            // function invocation
            register(nodeFactoryTable, new InfixInvocationFactory("(", Tokenizer.OPERATORS["("]));


            // array constructor

            // merged: register(new Prefix("[") {        
            register(nodeFactoryTable, new InfixArrayFactory("[", Tokenizer.OPERATORS["["]));

            // order-by
            register(nodeFactoryTable, new InfixOrderByFactory("^", Tokenizer.OPERATORS["^"]));

            register(nodeFactoryTable, new InfixBlockFactory("{", Tokenizer.OPERATORS["{"]));

            // bind variable
            register(nodeFactoryTable, new InfixRVariableBindFactory(":=", Tokenizer.OPERATORS[":="]));

            // focus variable bind
            register(nodeFactoryTable, new InfixFocusFactory("@", Tokenizer.OPERATORS["@"]));

            // index (position) variable bind
            register(nodeFactoryTable, new InfixIndexFactory("#", Tokenizer.OPERATORS["#"]));

            // if/then/else ternary operator ?:
            register(nodeFactoryTable, new InfixTernaryFactory("?", Tokenizer.OPERATORS["?"]));

            // elvis/default operator
            register(nodeFactoryTable, new InfixElvisFactory("?:", Tokenizer.OPERATORS["?:"]));

            // object transformer
            register(nodeFactoryTable, new PrefixTransformerFactory("|"));

            return nodeFactoryTable;
        }
    }
}
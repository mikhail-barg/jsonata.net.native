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
                if (this.currentNodeFactory.id == "(end)") 
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
                factory = Parser.s_nodeFactoryTable["(end)"];
                this.currentNodeFactory = factory;
                this.currentToken = new Token(SymbolType._end, null, source.Length);
                return;
            }
            this.currentToken = next_token;
            switch (this.currentToken.type) 
            {
            case SymbolType.name:
            case SymbolType.variable:
                factory = Parser.s_nodeFactoryTable["(name)"];
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
            case SymbolType.number:
            case SymbolType.value:
                factory = Parser.s_nodeFactoryTable["(literal)"];
                break;
            case SymbolType.regex:
                factory = Parser.s_nodeFactoryTable["(regex)"];
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
                Node thunk = new Node(); 
                thunk.type = SymbolType.lambda; 
                thunk.thunk = true; 
                thunk.arguments = new(); 
                thunk.position = expr.position;
                thunk.body = expr;
                result = thunk;
            } 
            else if (expr.type == SymbolType.condition) 
            {
                ConditionNode conditionExpr = (ConditionNode)expr;
                // analyse both branches
                conditionExpr.then = this.tailCallOptimize(conditionExpr.then!);
                if (conditionExpr.@else != null) 
                {
                    conditionExpr.@else = this.tailCallOptimize(conditionExpr.@else);
                }
                result = expr;
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
                // last step in path
                node.tuple = true;
                int index = node.steps!.Count - 1;
                slot = this.seekParent(node.steps[index--], slot);
                while (slot.level > 0 && index >= 0) 
                {
                    // check previous steps
                    slot = this.seekParent(node.steps[index--], slot);
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

        private void resolveAncestry(Node path) 
        {
            int index = path.steps!.Count - 1;
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
                switch (expr.value!.ToString())
                {
                case ".":
                    {
                        Node lstep = this.processAST(expr.lhs!);
                        if (lstep.type == SymbolType.path)
                        {
                            result = lstep;
                        }
                        else
                        {
                            result = new Node();
                            result.type = SymbolType.path;
                            result.steps = new() { lstep! };
                        }
                        if (lstep.type == SymbolType.parent)
                        {
                            result.seekingParent = new() { lstep.slot! };
                        }
                        Node rest = this.processAST(expr.rhs!);
                        if (rest.type == SymbolType.function &&
                            rest.procedure!.type == SymbolType.path &&
                            rest.procedure!.steps!.Count == 1 &&
                            rest.procedure!.steps[0].type == SymbolType.name &&
                            result.steps![result.steps.Count - 1].type == SymbolType.function
                        )
                        {
                            // next function in chain of functions - will override a thenable
                            result.steps[result.steps.Count - 1].nextFunction = (Node)rest.procedure.steps[0].value!;
                        }
                        if (rest.type == SymbolType.path)
                        {
                            result.steps!.AddRange(rest.steps!);
                        }
                        else
                        {
                            if (rest.predicate != null)
                            {
                                rest.stages = rest.predicate;
                                rest.predicate = null;
                            }
                            result.steps!.Add(rest);
                        }
                        // any steps within a path that are string literals, should be changed to 'name'
                        foreach (Node step in result.steps)
                        {
                            if (step.type == SymbolType.number || step.type == SymbolType.value)
                            {
                                // don't allow steps to be numbers or the values true/false/null
                                throw new JException("S0213", step.position, step.value);
                            }
                            if (step.type == SymbolType.@string)
                            {
                                step.type = SymbolType.name;
                            }
                        }

                        // any step that signals keeping a singleton array, should be flagged on the path
                        if (result.steps.Any(step => step.keepArray))
                        {
                            result.keepSingletonArray = true;
                        }
                        // if first step is a path constructor, flag it for special handling
                        Node firststep = result.steps[0];
                        if (firststep.type == SymbolType.unary && firststep.value!.Equals("["))
                        {
                            firststep.consarray = true;
                        }
                        // if the last step is an array constructor, flag it so it doesn't flatten
                        Node laststep = result.steps[result.steps.Count - 1];
                        if (laststep.type == SymbolType.unary && laststep.value!.Equals("["))
                        {
                            laststep.consarray = true;
                        }
                        this.resolveAncestry(result);
                    }
                    break;
                case "[":
                    {
                        // predicated step
                        // LHS is a step or a predicated step
                        // RHS is the predicate expr
                        result = this.processAST(expr.lhs!);
                        Node step = result;
                        SymbolType type = SymbolType.predicate;
                        if (result.type == SymbolType.path)
                        {
                            step = result.steps![result.steps.Count - 1];
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

                        Node predicate = this.processAST(expr.rhs!);
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
                        Node s = new Node();
                        s.type = SymbolType.filter;
                        s.expr = predicate;
                        s.position = expr.position;

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
                case "{":
                    {
                        // group-by
                        // LHS is a step or a predicated step
                        // RHS is the object constructor expr
                        result = this.processAST(expr.lhs!);
                        if (result == null)
                        {
                            throw new Exception("Should not happen?");
                        }
                        if (result.group != null)
                        {
                            throw new JException("S0210", expr.position);
                        }
                        // object constructor - process each pair
                        result.group = new Node();
                        result.group.lhsObject = expr.rhsObject!
                            .Select(pair => new Node[] { this.processAST(pair[0]), this.processAST(pair[1]) })
                            .ToList();
                        result.group.position = expr.position;
                    }
                    break;
                case "^":
                    {
                        // order-by
                        // LHS is the array to be ordered
                        // RHS defines the terms
                        result = this.processAST(expr.lhs!);
                        if (result.type != SymbolType.path)
                        {
                            Node _res = new Node();
                            _res.type = SymbolType.path;
                            _res.steps = new() { result };
                            result = _res;
                        }
                        Node sortStep = new Node();
                        sortStep.type = SymbolType.sort;
                        sortStep.position = expr.position;
                        sortStep.terms = expr.rhsTerms!.Select(terms => {
                            Node expression = this.processAST(terms.expression!);
                            this.pushAncestry(sortStep, expression);
                            Node res = new Node();
                            res.descending = terms.descending;
                            res.expression = expression;
                            return res;
                        }).ToList();
                        result.steps!.Add(sortStep);
                        this.resolveAncestry(result);
                    }
                    break;
                case ":=":
                    {
                        result = new Node();
                        result.type = SymbolType.bind;
                        result.value = expr.value;
                        result.position = expr.position;
                        result.lhs = this.processAST(expr.lhs!);
                        result.rhs = this.processAST(expr.rhs!);
                        this.pushAncestry(result, result.rhs!);
                    }
                    break;
                case "@":
                    {
                        result = this.processAST(expr.lhs!);
                        Node step = result;
                        if (result.type == SymbolType.path)
                        {
                            step = result.steps![result.steps.Count - 1];
                        }
                        // throw error if there are any predicates defined at this point
                        // at this point the only type of stages can be predicates
                        if (step.stages != null || step.predicate != null)
                        {
                            throw new JException("S0215", expr.position);
                        }
                        // also throw if this is applied after an 'order-by' clause
                        if (step.type == SymbolType.sort)
                        {
                            throw new JException("S0216", expr.position);
                        }
                        if (expr.keepArray)
                        {
                            step.keepArray = true;
                        }
                        step.focus = (string)expr.rhs!.value!;
                        step.tuple = true;
                    }
                    break;
                case "#":
                    {
                        result = processAST(expr.lhs!);
                        Node step = result;
                        if (result.type == SymbolType.path)
                        {
                            step = result.steps![result.steps.Count - 1];
                        }
                        else
                        {
                            Node _res = new Node();
                            _res.type = SymbolType.path;
                            _res.steps = new() { result };
                            result = _res;
                            if (step.predicate != null)
                            {
                                step.stages = step.predicate;
                                step.predicate = null;
                            }
                        }
                        if (step.stages == null)
                        {
                            step.index_string = (string)expr.rhs!.value!; // name of index variable = String
                        }
                        else
                        {
                            Node _res = new Node();
                            _res.type = SymbolType.index;
                            _res.value = expr.rhs!.value;
                            _res.position = expr.position;
                            step.stages.Add(_res);
                        }
                        step.tuple = true;
                    }
                    break;
                case "~>":
                    {
                        result = new Node();
                        result.type = SymbolType.apply;
                        result.value = expr.value;
                        result.position = expr.position;
                        result.lhs = processAST(expr.lhs!);
                        result.rhs = processAST(expr.rhs!);
                        result.keepArray = result.lhs.keepArray || result.rhs.keepArray;
                    }
                    break;
                default:
                    {
                        Node _result = new Node();
                        _result.type = expr.type;
                        _result.value = expr.value;
                        _result.position = expr.position;
                        _result.lhs = this.processAST((expr).lhs!);
                        _result.rhs = this.processAST((expr).rhs!);
                        this.pushAncestry(_result, _result.lhs);
                        this.pushAncestry(_result, _result.rhs);
                        result = _result;
                    }
                    break;
                }
                break; // binary

            case SymbolType.unary:
                {
                    result = new Node();
                    result.type = expr.type;
                    result.value = expr.value;
                    result.position = expr.position;
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
                            Node key = this.processAST(pair[0]);
                            this.pushAncestry(result, key);
                            Node value = this.processAST(pair[1]);
                            this.pushAncestry(result, value);
                            return new Node[] { key, value };
                        }).ToList();
                    } else {
                        // all other unary expressions - just process the expression
                        result.expression = this.processAST(expr.expression!);
                        // if unary minus on a number, then pre-process
                        if (exprValue == "-" && result.expression.type == SymbolType.number)
                        {
                            result = result.expression;
                            if (result.value is long longValue)
                            {
                                result.value = -longValue;
                            }
                            else if (result.value is double doubleValue)
                            {
                                result.value = -doubleValue;
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
                    result = new Node();
                    result.type = expr.type;
                    result.name = expr.name;
                    result.value = expr.value;
                    result.position = expr.position;
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
                    result = new Node();
                    result.type = expr.type;
                    result.arguments = expr.arguments;
                    result.signature = expr.signature;
                    result.position = expr.position;
                    Node body = this.processAST(expr.body!);
                    result.body = this.tailCallOptimize(body);
                }
                break;
            case SymbolType.condition:
                {
                    ConditionNode exprCondition = (ConditionNode)expr;
                    ConditionNode resultCondition = new ConditionNode();
                    resultCondition.type = expr.type;
                    resultCondition.position = expr.position;
                    resultCondition.condition = this.processAST(exprCondition.condition!);
                    this.pushAncestry(resultCondition, resultCondition.condition);
                    resultCondition.then = this.processAST(exprCondition.then!);
                    this.pushAncestry(resultCondition, resultCondition.then);
                    if (exprCondition.@else != null)
                    {
                        resultCondition.@else = this.processAST(exprCondition.@else);
                        this.pushAncestry(resultCondition, resultCondition.@else);
                    }
                    result = resultCondition;
                }
                break;
            case SymbolType.transform:
                {
                    result = new Node();
                    result.type = expr.type; result.position = expr.position;
                    result.pattern = this.processAST(expr.pattern!);
                    result.update = this.processAST(expr.update!);
                    if (expr.delete != null)
                    {
                        result.delete = this.processAST(expr.delete);
                    }
                }
                break;
            case SymbolType.block:
                {
                    result = new Node();
                    result.type = expr.type; result.position = expr.position;
                    // array of expressions - process each one
                    result.expressions = expr.expressions!.Select(item => {
                        Node part = this.processAST(item);
                        this.pushAncestry(result, part);
                        if (part.consarray || (part.type == SymbolType.path && part.steps![0].consarray)) 
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
                    result = new Node();
                    result.type = SymbolType.path;
                    result.steps = new() { expr };
                    if (expr.keepArray)
                    {
                        result.keepSingletonArray = true;
                    }
                }
                break;
            case SymbolType.parent:
                {
                    result = new Node();
                    result.type = SymbolType.parent;
                    result.slot = new Node();
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
                        expr.type = SymbolType.name;
                        result = this.processAST(expr);
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
            case SymbolType.error:
                result = expr;
                if (expr.lhs != null) 
                {
                    result = this.processAST(expr.lhs);
                }
                break;
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

        internal Node objectParser(Node? left) 
        {
            Node res = new Node() { value = "{" };
            List<Node[]> a = new ();
            if (this.currentNodeFactory.id != "}") 
            {
                while (true)
                {
                    Node n = this.expression(0);
                    this.advance(":");
                    Node v = this.expression(0);
                    Node[] pair = new Node[] { n, v };
                    a.Add( pair ); // holds an array of name/value expression pairs
                    if (this.currentNodeFactory.id != ",") 
                    {
                        break;
                    }
                    this.advance(",");
                }
            }
            this.advance("}", true);
            if (left == null) 
            {
                // NUD - unary prefix form
                res.lhsObject = a;
                res.type = SymbolType.unary;
            } 
            else 
            {
                // LED - binary infix form
                res.lhs = left;
                res.rhsObject = a;
                res.type = SymbolType.binary;
            }
            return res;
        }

        internal Node parse(string jsonata) 
        {
            this.source = jsonata;

            // now invoke the tokenizer and the parser and return the syntax tree
            this.lexer = new Tokenizer(source);
            this.advance();
            // parse the tokens
            Node expr = this.expression(0);
            if (this.currentNodeFactory.id != "(end)") 
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

        private static Dictionary<string, NodeFactoryBase> s_nodeFactoryTable = CreateNodeFactoryTable();

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
            register(nodeFactoryTable, new TerminalFactory("(end)"));
            register(nodeFactoryTable, new TerminalFactory("(name)"));
            register(nodeFactoryTable, new TerminalFactory("(literal)"));
            register(nodeFactoryTable, new TerminalFactory("(regex)"));
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

            register(nodeFactoryTable, new InfixWithNudFactory("and")); // allow as terminal // Boolean AND
            register(nodeFactoryTable, new InfixWithNudFactory("or")); // allow as terminal // Boolean OR
            register(nodeFactoryTable, new InfixWithNudFactory("in")); // allow as terminal // is member of array
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
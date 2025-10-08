using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Jsonata.Net.Native.Json;

namespace Jsonata.Net.Native.New
{ 
    internal class Parser 
    {
        // This parser implements the 'Top down operator precedence' algorithm developed by Vaughan R Pratt; http://dl.acm.org/citation.cfm?id=512931.
        // and builds on the Javascript framework described by Douglas Crockford at http://javascript.crockford.com/tdop/tdop.html
        // and in 'Beautiful Code', edited by Andy Oram and Greg Wilson, Copyright 2007 O'Reilly Media, Inc. 798-0-596-51004-6

        private string source = default!;
        private Tokenizer lexer = default!;

        //var parser = function (source, recover) {
        internal Symbol node { get; private set; } = default!;

        internal Symbol advance() 
        { 
            return this.advance(null); 
        }
        
        internal Symbol advance(string? id) 
        { 
            return this.advance(id, false); 
        }

        internal Symbol advance(string? id, bool infix) 
        {
            if (id != null && this.node.id != id) 
            {
                String code;
                if (this.node.id == "(end)") 
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
                    this.node.position,
                    id,
                    this.node.value
                );
            }
            Token? next_token = lexer.next(infix);
            //if (dbg) System.out.println("nextToken "+(next_token!=null ? next_token.type : null));
            SymbolFactoryBase factory;
            if (next_token == null) 
            {
                factory = this.m_symbolFactoryTable["(end)"];
                this.node = factory.Invoke();
                this.node.position = source.Length;
                return this.node;
            }
            object? value = next_token.value;
            SymbolType type = next_token.type;
            switch (type) 
            {
            case SymbolType.name:
            case SymbolType.variable:
                factory = this.m_symbolFactoryTable["(name)"];
                break;
            case SymbolType.@operator:
                if (!this.m_symbolFactoryTable.TryGetValue(value!.ToString()!, out SymbolFactoryBase? foundFactory))
                {
                    throw new JException("S0204", next_token.position, value);
                }
                else
                {
                    factory = foundFactory;
                }
                break;
            case SymbolType.@string:
            case SymbolType.number:
            case SymbolType.value:
                factory = m_symbolFactoryTable["(literal)"];
                break;
            case SymbolType.regex:
                factory = m_symbolFactoryTable["(regex)"];
                break;
            /* istanbul ignore next */
            default:
                throw new JException("S0205", next_token.position, value);
            }

            this.node = factory.Invoke();
            this.node.value = value;
            this.node.type = type;
            this.node.position = next_token.position;
            return node;
        }

        // Pratt's algorithm
        internal Symbol expression(int rbp) 
        {
            Symbol left;
            Symbol t = this.node;
            this.advance(null, true);
            left = t.nud(this);
            while (rbp < this.node.bp) //was LBP
            {
                t = this.node;
                advance(null, false);
                //if (dbg) System.out.println("t="+t+", left="+left.type);
                left = t.led(left, this);
            }
            return left;
        }

        // tail call optimization
        // this is invoked by the post parser to analyse lambda functions to see
        // if they make a tail call.  If so, it is replaced by a thunk which will
        // be invoked by the trampoline loop during function application.
        // This enables tail-recursive functions to be written without growing the stack
        private Symbol tailCallOptimize(Symbol expr) 
        {
            Symbol result;
            if (expr.type == SymbolType.function && expr.predicate == null) 
            {
                Symbol thunk = new Symbol(); 
                thunk.type = SymbolType.lambda; 
                thunk.thunk = true; 
                thunk.arguments = new(); 
                thunk.position = expr.position;
                thunk.body = expr;
                result = thunk;
            } 
            else if (expr.type == SymbolType.condition) 
            {
                // analyse both branches
                expr.then = this.tailCallOptimize(expr.then!);
                if (expr.@else != null) 
                {
                    expr.@else = this.tailCallOptimize(expr.@else);
                }
                result = expr;
            } 
            else if (expr.type == SymbolType.block) 
            {
                // only the last expression in the block
                int length = expr.expressions!.Count;
                if (length > 0) 
                {
                    //if (!(expr.expressions is ArrayList))
                    //{
                    //    expr.expressions = new ArrayList<>(expr.expressions);
                    //}
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
        List<Symbol> ancestry = new();

        private Symbol seekParent(Symbol node, Symbol slot) 
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
                        this.ancestry[(int)slot.index!].slot!.label = node.ancestor.label;
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

        private void pushAncestry(Symbol result, Symbol? value) 
        {
            if (value == null)
            {
                return; // Added NPE check
            }
            if (value.seekingParent != null || value.type == SymbolType.parent) 
            {
                List<Symbol> slots = value.seekingParent ?? new();
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

        private void resolveAncestry(Symbol path) 
        {
            int index = path.steps!.Count - 1;
            Symbol laststep = path.steps[index];
            List<Symbol> slots = laststep.seekingParent ?? new();
            if (laststep.type == SymbolType.parent) 
            {
                slots.Add(laststep.slot!);
            }
            for (int i = 0; i < slots.Count; ++i) 
            {
                Symbol slot = slots[i];
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
                    Symbol step = path.steps[index--];
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
        private Symbol processAST(Symbol? expr) 
        {
            Symbol? result = expr;
            if (expr == null)
            {
                throw new Exception("Should not happen, or NUIll should be a Symbol");
            }
            // if (dbg) System.out.println(" > processAST type="+expr.type+" value='"+expr.value+"'");
            switch (expr.type)
            {
            case SymbolType.binary:
                switch (expr.value!.ToString())
                {
                case ".":
                    {
                        Symbol lstep = this.processAST(((Infix)expr).lhs);
                        if (lstep.type == SymbolType.path)
                        {
                            result = lstep;
                        }
                        else
                        {
                            result = new InfixCustom();
                            result.type = SymbolType.path;
                            result.steps = new() { lstep! };
                            //result = {type: 'path', steps: [lstep]};
                        }
                        if (lstep.type == SymbolType.parent)
                        {
                            result.seekingParent = new() { lstep.slot! };
                        }
                        Symbol rest = this.processAST(((Infix)expr).rhs);
                        if (rest.type == SymbolType.function &&
                            rest.procedure!.type == SymbolType.path &&
                            rest.procedure!.steps!.Count == 1 &&
                            rest.procedure!.steps[0].type == SymbolType.name &&
                            result.steps![result.steps.Count - 1].type == SymbolType.function
                        )
                        {
                            // next function in chain of functions - will override a thenable
                            result.steps[result.steps.Count - 1].nextFunction = (Symbol)rest.procedure.steps[0].value!;
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
                                //delete rest.predicate;
                            }
                            result.steps!.Add(rest);
                        }
                        // any steps within a path that are string literals, should be changed to 'name'
                        foreach (Symbol step in result.steps)
                        {
                            if (step.type == SymbolType.number || step.type == SymbolType.value)
                            {
                                // don't allow steps to be numbers or the values true/false/null
                                throw new JException("S0213", step.position, step.value);
                            }
                            //System.out.println("step "+step+" type="+step.type);
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
                        Symbol firststep = result.steps[0];
                        if (firststep.type == SymbolType.unary && firststep.value!.Equals("["))
                        {
                            firststep.consarray = true;
                        }
                        // if the last step is an array constructor, flag it so it doesn't flatten
                        Symbol laststep = result.steps[result.steps.Count - 1];
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
                        result = this.processAST(((Infix)expr).lhs);
                        Symbol step = result;
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

                        Symbol predicate = this.processAST(((Infix)expr).rhs);
                        if (predicate.seekingParent != null)
                        {
                            foreach (Symbol slot in predicate.seekingParent)
                            {
                                if (slot.level == 1) {
                                    this.seekParent(step, slot);
                                } else {
                                    --slot.level;
                                }
                            }
                            this.pushAncestry(step, predicate);
                        }
                        Symbol s = new Symbol();
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
                        result = this.processAST(expr.lhs);
                        if (result == null)
                        {
                            throw new Exception("Should not happen?");
                        }
                        if (result.group != null)
                        {
                            throw new JException("S0210", expr.position);
                        }
                        // object constructor - process each pair
                        result.group = new Symbol();
                        result.group.lhsObject = expr.rhsObject!
                            .Select(pair => new Symbol[] { this.processAST(pair[0]), this.processAST(pair[1]) })
                            .ToList();
                        result.group.position = expr.position;
                    }
                    break;
                case "^":
                    {
                        // order-by
                        // LHS is the array to be ordered
                        // RHS defines the terms
                        result = this.processAST(expr.lhs);
                        if (result.type != SymbolType.path)
                        {
                            Symbol _res = new Symbol();
                            _res.type = SymbolType.path;
                            _res.steps = new() { result };
                            result = _res;
                        }
                        Symbol sortStep = new Symbol();
                        sortStep.type = SymbolType.sort;
                        sortStep.position = expr.position;
                        sortStep.terms = expr.rhsTerms!.Select(terms => {
                            Symbol expression = this.processAST(terms.expression!);
                            this.pushAncestry(sortStep, expression);
                            Symbol res = new Symbol();
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
                        result = new Symbol();
                        result.type = SymbolType.bind;
                        result.value = expr.value;
                        result.position = expr.position;
                        result.lhs = this.processAST(expr.lhs);
                        result.rhs = this.processAST(expr.rhs);
                        this.pushAncestry(result, result.rhs);
                    }
                    break;
                case "@":
                    {
                        result = this.processAST(expr.lhs);
                        Symbol step = result;
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
                        step.focus = (JToken?)expr.rhs!.value;
                        step.tuple = true;
                    }
                    break;
                case "#":
                    {
                        result = processAST(expr.lhs);
                        Symbol step = result;
                        if (result.type == SymbolType.path)
                        {
                            step = result.steps![result.steps.Count - 1];
                        }
                        else
                        {
                            Symbol _res = new Symbol();
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
                            step.index = expr.rhs!.value; // name of index variable = String
                        }
                        else
                        {
                            Symbol _res = new Symbol();
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
                        result = new Symbol();
                        result.type = SymbolType.apply;
                        result.value = expr.value;
                        result.position = expr.position;
                        result.lhs = processAST(expr.lhs);
                        result.rhs = processAST(expr.rhs);
                        result.keepArray = result.lhs.keepArray || result.rhs.keepArray;
                    }
                    break;
                default:
                    {
                        Infix _result = new InfixCustom();
                        _result.type = expr.type;
                        _result.value = expr.value;
                        _result.position = expr.position;
                        _result.lhs = this.processAST((expr).lhs);
                        _result.rhs = this.processAST((expr).rhs);
                        this.pushAncestry(_result, _result.lhs);
                        this.pushAncestry(_result, _result.rhs);
                        result = _result;
                    }
                    break;
                }
                break; // binary

            case SymbolType.unary:
                {
                    result = new Symbol();
                    result.type = expr.type;
                    result.value = expr.value;
                    result.position = expr.position;
                    // expr.value might be Character!
                    string exprValue = expr.value!.ToString()!;
                    if (exprValue == "[")
                    {
                        // array constructor - process each item
                        result.expressions = expr.expressions!.Select(item => {
                            Symbol value = this.processAST(item);
                            this.pushAncestry(result, value);
                            return value;
                        }).ToList();
                    }
                    else if (exprValue == "{")
                    {
                        // object constructor - process each pair
                        //throw new Error("processAST {} unimpl");
                        result.lhsObject = expr.lhsObject!.Select(pair => {
                            Symbol key = this.processAST(pair[0]);
                            this.pushAncestry(result, key);
                            Symbol value = this.processAST(pair[1]);
                            this.pushAncestry(result, value);
                            return new Symbol[] { key, value };
                        }).ToList();
                    } else {
                        // all other unary expressions - just process the expression
                        result.expression = this.processAST(expr.expression);
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
                    result = new Symbol();
                    result.type = expr.type;
                    result.name = expr.name;
                    result.value = expr.value;
                    result.position = expr.position;
                    result.arguments = expr.arguments!.Select(arg => {
                        Symbol argAST = this.processAST(arg);
                        this.pushAncestry(result, argAST);
                        return argAST;
                    }).ToList();
                    result.procedure = processAST(expr.procedure);
                }
                break;
            case SymbolType.lambda:
                {
                    result = new Symbol();
                    result.type = expr.type;
                    result.arguments = expr.arguments;
                    result.signature = expr.signature;
                    result.position = expr.position;
                    Symbol body = this.processAST(expr.body);
                    result.body = this.tailCallOptimize(body);
                }
                break;
            case SymbolType.condition:
                {
                    result = new Symbol();
                    result.type = expr.type; 
                    result.position = expr.position;
                    result.condition = this.processAST(expr.condition);
                    this.pushAncestry(result, result.condition);
                    result.then = this.processAST(expr.then);
                    this.pushAncestry(result, result.then);
                    if (expr.@else != null)
                    {
                        result.@else = this.processAST(expr.@else);
                        this.pushAncestry(result, result.@else);
                    }
                }
                break;
            case SymbolType.transform:
                {
                    result = new Symbol();
                    result.type = expr.type; result.position = expr.position;
                    result.pattern = this.processAST(expr.pattern);
                    result.update = this.processAST(expr.update);
                    if (expr.delete != null)
                    {
                        result.delete = this.processAST(expr.delete);
                    }
                }
                break;
            case SymbolType.block:
                {
                    result = new Symbol();
                    result.type = expr.type; result.position = expr.position;
                    // array of expressions - process each one
                    result.expressions = expr.expressions!.Select(item => {
                        Symbol part = this.processAST(item);
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
                    result = new Symbol();
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
                    result = new Symbol();
                    result.type = SymbolType.parent;
                    result.slot = new Symbol();
                    result.slot.label = "!" + ++ancestorLabel;
                    result.slot.level = 1;
                    result.slot.index = ++ancestorIndex;
                    //slot: { label: '!' + ancestorLabel++, level: 1, index: ancestorIndex++ } };
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
                    if (expr.id == "(end)")
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

        internal Symbol objectParser(Symbol? left) 
        {
            Symbol res = left != null ? new Infix("{") : new Prefix("{");
            List<Symbol[]> a = new ();
            if (this.node.id != "}") 
            {
                while (true)
                {
                    Symbol n = this.expression(0);
                    this.advance(":");
                    Symbol v = this.expression(0);
                    Symbol[] pair = new Symbol[] { n, v };
                    a.Add( pair ); // holds an array of name/value expression pairs
                    if (this.node.id != ",") 
                    {
                        break;
                    }
                    this.advance(",");
                }
            }
            this.advance("}", true);
            if (left == null) 
            { //typeof left === 'undefined') {
                // NUD - unary prefix form
                ((Prefix)res).lhsObject = a;
                ((Prefix)res).type = SymbolType.unary;
            } 
            else 
            {
                // LED - binary infix form
                ((Infix)res).lhs = left;
                ((Infix)res).rhsObject = a;
                ((Infix)res).type = SymbolType.binary;
            }
            return res;
        }

        public Symbol parse(string jsonata) 
        {
            this.source = jsonata;

            // now invoke the tokenizer and the parser and return the syntax tree
            this.lexer = new Tokenizer(source);
            this.advance();
            // parse the tokens
            Symbol expr = this.expression(0);
            if (this.node.id != "(end)") 
            {
                throw new JException("S0201", this.node.position, this.node.value);
            }

            expr = this.processAST(expr);

            if (expr.type == SymbolType.parent || expr.seekingParent != null) 
            {
                // error - trying to derive ancestor at top level
                throw new JException("S0217", expr.position, expr.type);
            }

            return expr;
        }

        //This was Symbol.create()
        private abstract class SymbolFactoryBase
        {
            internal readonly string id;
            internal abstract Symbol Invoke();

            internal SymbolFactoryBase(string id)
            {
                this.id = id;
            }
        }

        private sealed class TerminalFactory : SymbolFactoryBase
        {
            public TerminalFactory(string id) : base(id)
            {
            }

            internal override Symbol Invoke()
            {
                return new Terminal(this.id);
            }
        }

        private sealed class SymbolFactory : SymbolFactoryBase
        {
            public SymbolFactory(string id) : base(id)
            {
            }

            internal override Symbol Invoke()
            {
                return new Symbol(this.id);
            }
        }

        private sealed class InfixFactory : SymbolFactoryBase
        {
            public InfixFactory(string id) : base(id)
            {
            }

            internal override Symbol Invoke()
            {
                return new Infix(this.id);
            }
        }

        private sealed class InfixWithNudFactory : SymbolFactoryBase
        {
            public InfixWithNudFactory(string id) : base(id)
            {
            }

            internal override Symbol Invoke()
            {
                return new InfixWithNud(this.id);
            }
        }

        private sealed class InfixWithTypedNudFactory : SymbolFactoryBase
        {
            private readonly SymbolType m_symbolType;

            public InfixWithTypedNudFactory(string id, SymbolType symbolType) : base(id)
            {
                this.m_symbolType = symbolType;
            }

            internal override Symbol Invoke()
            {
                return new InfixWithTypedNud(this.id, this.m_symbolType);
            }
        }

        private sealed class CustomFactory : SymbolFactoryBase
        {
            private readonly Func<Symbol> m_factory;

            public CustomFactory(string id, Func<Symbol> factory) : base(id)
            {
                this.m_factory = factory;
            }

            internal override Symbol Invoke()
            {
                return this.m_factory.Invoke();
            }
        }

        private Dictionary<string, SymbolFactoryBase> m_symbolFactoryTable = new();

        private void register(SymbolFactoryBase t)
        {
            //if (t instanceof Infix || t instanceof InfixR) return;

            /*

            if (this.m_symbolFactoryTable.TryGetValue(t.id, out Symbol? s))
            {
                //if (dbg) System.out.println("Symbol in table "+t.id+" "+s.getClass().getName()+" -> "+ t.getClass().getName());
                //symbolTable.put(t.id, t);
                if (t.bp >= s.lbp)
                {
                    //if (dbg) System.out.println("Symbol in table "+t.id+" lbp="+s.lbp+" -> "+t.bp);
                    s.lbp = t.bp;
                }
            }
            else
            {
                s = t.create();
                s.value = s.id = t.id;
                s.lbp = t.bp;
                this.m_symbolFactoryTable.Add(t.id, s);
            }
            */

            if (this.m_symbolFactoryTable.TryGetValue(t.id, out SymbolFactoryBase? s))
            {
                throw new Exception("Handle combine?? " + t.id);
            }
            else
            {
                this.m_symbolFactoryTable.Add(t.id, t);
            }
        }


        public Parser() 
        {
            register(new TerminalFactory("(end)"));
            register(new TerminalFactory("(name)"));
            register(new TerminalFactory("(literal)"));
            register(new TerminalFactory("(regex)"));
            register(new SymbolFactory(":"));
            register(new SymbolFactory(";"));
            register(new SymbolFactory(","));
            register(new SymbolFactory(")"));
            register(new SymbolFactory("]"));
            register(new SymbolFactory("}"));
            register(new SymbolFactory("..")); // range operator
            register(new InfixFactory(".")); // map operator
            register(new InfixFactory("+")); // numeric addition
            register(new CustomFactory("-", () => new InfixAndPrefix("-"))); // numeric subtraction // unary numeric negation

            register(new InfixWithTypedNudFactory("*", SymbolType.wildcard)); // field wildcard (single level) // numeric multiplication
            register(new InfixFactory("/")); // numeric division
            register(new InfixWithTypedNudFactory("%", SymbolType.parent)); // parent operator // numeric modulus
            register(new InfixFactory("=")); // equality
            register(new InfixFactory("<")); // less than
            register(new InfixFactory(">")); // greater than
            register(new InfixFactory("!=")); // not equal to
            register(new InfixFactory("<=")); // less than or equal
            register(new InfixFactory(">=")); // greater than or equal
            register(new InfixFactory("&")); // string concatenation

            register(new InfixWithNudFactory("and")); // allow as terminal // Boolean AND
            register(new InfixWithNudFactory("or")); // allow as terminal // Boolean OR
            register(new InfixWithNudFactory("in")); // allow as terminal // is member of array
            // merged Infix: register(new Terminal("and")); // the 'keywords' can also be used as terminals (field names)
            // merged Infix: register(new Terminal("or")); //
            // merged Infix: register(new Terminal("in")); //
            // merged Infix: register(new Prefix("-")); // unary numeric negation
            register(new InfixFactory("~>")); // function application

            // coalescing operator
            register(new CustomFactory("??", () => new InfixCoalescing("??", Tokenizer.operators["??"])));

            register(new CustomFactory("(error)", () => new InfixRError("(error)", 10)));

            // field wildcard (single level)
            // merged with Infix *
            // register(new Prefix("*") {
            //     @Override Symbol nud() {
            //         type = "wildcard";
            //         return this;
            //     }
            // });

            // descendant wildcard (multi-level)

            register(new CustomFactory("**", () => new PrefixDescendantWindcard("**")));

            // parent operator
            // merged with Infix %
            // register(new Prefix("%") {
            //     @Override Symbol nud() {
            //         type = "parent";
            //         return this;
            //     }
            // });

            // function invocation
            register(new CustomFactory("(", () => new InfixInvocation("(", Tokenizer.operators["("])));


            // array constructor

            // merged: register(new Prefix("[") {        
            register(new CustomFactory("[", () => new InfixArray("[", Tokenizer.operators["["])));

            // order-by
            register(new CustomFactory("^", () => new InfixOrderBy("^", Tokenizer.operators["^"])));

            register(new CustomFactory("{", () => new InfixBlock("{", Tokenizer.operators["{"])));

            // bind variable
            register(new CustomFactory(":=", () => new InfixRVariableBind(":=", Tokenizer.operators[":="])));

            // focus variable bind
            register(new CustomFactory("@", () => new InfixFocus("@", Tokenizer.operators["@"])));

            // index (position) variable bind
            register(new CustomFactory("#", () => new InfixIndex("#", Tokenizer.operators["#"])));

            // if/then/else ternary operator ?:
            register(new CustomFactory("?", () => new InfixTernary("?", Tokenizer.operators["?"])));

            // elvis/default operator
            register(new CustomFactory("?:", () => new InfixElvis("?:", Tokenizer.operators["?:"])));

            // object transformer
            register(new CustomFactory("|", () => new PrefixTransformer("|")));
        }
    }
}
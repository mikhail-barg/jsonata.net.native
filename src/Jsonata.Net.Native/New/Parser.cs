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
                if (!Parser.s_binaryFactoryTable.TryGetValue(this.currentToken.value!.ToString()!, out NodeFactoryBase? foundFactory))
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
            case SymbolType._number_double:
                factory = Parser.s_terminalFactoryNumberDouble;
                break;
            case SymbolType._number_int:
                factory = Parser.s_terminalFactoryNumberInt;
                break;
            case SymbolType._value_bool:
                factory = Parser.s_terminalFactoryValueBool;
                break;
            case SymbolType._value_null:
                factory = Parser.s_terminalFactoryValueNull;
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
                Node thunk = new LambdaNode(expr.position, arguments: new(), signature: null, body: expr, thunk: true);
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
                BlockNode blockExpr = (BlockNode)expr;
                int length = blockExpr.expressions.Count;
                if (length > 0) 
                {
                    blockExpr.expressions[^1] = this.tailCallOptimize(blockExpr.expressions[^1]);
                }
                result = blockExpr;
            } 
            else 
            {
                result = expr;
            }
            return result;
        }

        int ancestorLabel = 0;
        int ancestorIndex = 0;
        List<ParentWithSlotNode> ancestry = new();

        private SlotNode seekParent(Node node, SlotNode slot) 
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
                        this.ancestry[slot.index_int].slot.label = node.ancestor.label;
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
                {
                    BlockNode exprBlock = (BlockNode)node;
                    if (exprBlock.expressions.Count > 0)
                    {
                        exprBlock.tuple = true;
                        slot = this.seekParent(exprBlock.expressions[^1], slot);
                    }
                }
                break;
            case SymbolType.path:
                {
                    // last step in path
                    PathNode pathNode = (PathNode)node;
                    node.tuple = true;
                    int index = pathNode.steps.Count - 1;
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
                List<SlotNode> slots = value.seekingParent ?? new();
                if (value.type == SymbolType.parent) 
                {
                    slots.Add(((ParentWithSlotNode)value).slot);
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
            List<SlotNode> slots = laststep.seekingParent ?? new();
            if (laststep.type == SymbolType.parent) 
            {
                slots.Add(((ParentWithSlotNode)laststep).slot);
            }
            for (int i = 0; i < slots.Count; ++i) 
            {
                SlotNode slot = slots[i];
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
                            resultPath.seekingParent = new() { ((ParentWithSlotNode)lstep).slot };
                        }
                        Node rest = this.processAST(exprBinary.rhs);
                        /* see https://github.com/jsonata-js/jsonata/issues/769
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
                        */
                        if (rest.type == SymbolType.path)
                        {
                            resultPath.steps.AddRange(((PathNode)rest).steps);
                        }
                        else
                        {
                            if (rest.predicate != null)
                            {
                                rest.stages = rest.predicate.OfType<StageNode>().ToList();
                                rest.predicate = null;
                            }
                            resultPath.steps.Add(rest);
                        }
                        // any steps within a path that are string literals, should be changed to 'name'
                        for (int i = 0; i < resultPath.steps.Count; ++i)
                        {
                            Node step = resultPath.steps[i];
                            if (step.type == SymbolType._number_double 
                                || step.type == SymbolType._number_int 
                                || step.type == SymbolType._value_bool
                                || step.type == SymbolType._value_null
                            )
                            {
                                // don't allow steps to be numbers or the values true/false/null
                                throw new JException("S0213", step.position, null /*step.value*/);
                            }
                            if (step.type == SymbolType.@string)
                            {
                                //step.type = SymbolType.name;
                                resultPath.steps[i] = new NameNode(step.position, ((StringNode)step).value);
                            }
                        }

                        // any step that signals keeping a singleton array, should be flagged on the path
                        if (resultPath.steps.Any(step => step.keepArray))
                        {
                            resultPath.keepSingletonArray = true;
                        }
                        // if first step is a path constructor, flag it for special handling
                        Node firststep = resultPath.steps[0];
                        if (firststep.type == SymbolType._unary_array)
                        {
                            firststep.consarray = true;
                        }
                        // if the last step is an array constructor, flag it so it doesn't flatten
                        Node laststep = resultPath.steps[^1];
                        if (laststep.type == SymbolType._unary_array)
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
                        bool typeIsStages;      //if false -> type is 'predicate'
                        if (result.type == SymbolType.path)
                        {
                            step = ((PathNode)result).steps[^1];
                            typeIsStages = true;  // type = 'stages'
                        }
                        else
                        {
                            typeIsStages = false; // type = 'predicate'
                        }
                        if (step.group != null)
                        {
                            throw new JException("S0209", expr.position);
                        }

                        Node predicate = this.processAST(exprBinary.rhs);
                        if (predicate.seekingParent != null)
                        {
                            foreach (SlotNode slot in predicate.seekingParent)
                            {
                                if (slot.level == 1) 
                                {
                                    this.seekParent(step, slot);
                                } 
                                else 
                                {
                                    --slot.level;
                                }
                            }
                            this.pushAncestry(step, predicate);
                        }
                        FilterNode filter = new FilterNode(expr.position, predicate);

                        // if (typeof step[type] === 'undefined') {
                        //    step[type] = [];
                        // }
                        //step[type].push({type: 'filter', expr: predicate, position: expr.position});
                        if (typeIsStages)
                        {
                            if (step.stages == null)
                            {
                                step.stages = new();
                            }
                            step.stages.Add(filter);
                        }
                        else
                        {
                            if (step.predicate == null)
                            {
                                step.predicate = new();
                            }
                            step.predicate.Add(filter);
                        }
                        
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
                        result = new BinaryNode(((BinaryNode)expr).value, expr.position, lhs, rhs); //TODO: binary node
                        this.pushAncestry(result, lhs);
                        this.pushAncestry(result, rhs);
                    }
                    break;
                }
                break; // binary
            case SymbolType._binary_bind_assign:
                {
                    BindAssignVarNode exprBind = (BindAssignVarNode)expr;
                    Node lhs = this.processAST(exprBind.lhs);
                    Node rhs = this.processAST(exprBind.rhs);
                    if (lhs.type != SymbolType.variable)
                    {
                        throw new Exception("Should not happen, because exprBind.lhs was variable!");
                    }
                    result = new BindNode(exprBind.position, (VariableNode)lhs, rhs);
                    this.pushAncestry(result, rhs);
                }
                break;
            case SymbolType._binary_bind_context:
                {
                    BindContextVarNode exprBind = (BindContextVarNode)expr;
                    result = this.processAST(exprBind.lhs);
                    Node step = result;
                    if (result.type == SymbolType.path)
                    {
                        step = ((PathNode)result).steps[^1];
                    }
                    // throw error if there are any predicates defined at this point
                    // at this point the only type of stages can be predicates
                    if (step.stages != null || step.predicate != null)
                    {
                        throw new JException("S0215", exprBind.position);
                    }
                    // also throw if this is applied after an 'order-by' clause
                    if (step.type == SymbolType.sort)
                    {
                        throw new JException("S0216", exprBind.position);
                    }
                    if (exprBind.keepArray)
                    {
                        step.keepArray = true;
                    }
                    step.focus = exprBind.rhs.value;
                    step.tuple = true;
                }
                break;
            case SymbolType._binary_bind_positional:
                {
                    BindPositionalVarNode exprBind = (BindPositionalVarNode)expr;
                    result = processAST(exprBind.lhs);
                    Node step;
                    if (result.type == SymbolType.path)
                    {
                        step = ((PathNode)result).steps[^1];
                    }
                    else
                    {
                        step = result;
                        result = new PathNode(new() { step });
                        if (step.predicate != null)
                        {
                            step.stages = step.predicate.OfType<StageNode>().ToList();
                            step.predicate = null;
                        }
                    }
                    if (step.stages == null)
                    {
                        step.index_string = exprBind.rhs.value;
                    }
                    else
                    {
                        IndexNode _res = new IndexNode(expr.position, exprBind.rhs.value);
                        step.stages.Add(_res);
                    }
                    step.tuple = true;
                }
                break;
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
                    List<Tuple<Node, Node>> lhsObject = exprGroupby.rhsObject
                        .Select(pair => Tuple.Create(this.processAST(pair.Item1), this.processAST(pair.Item2)))
                        .ToList();
                    result.group = new GroupNode(expr.position, lhsObject);
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
                    SortNode sortStep = new SortNode(expr.position, terms: new() );
                    foreach (SortTermNode term in exprOrderby.rhsTerms)
                    {
                        Node expression = this.processAST(term.expression!);
                        this.pushAncestry(sortStep, expression);
                        SortTermNode newTerm = new SortTermNode(term.position, expression, term.descending);
                        sortStep.terms.Add(newTerm);
                    }
                    resultPath.steps.Add(sortStep);
                    result = resultPath;
                    this.resolveAncestry(resultPath);
                }
                break; // _tinary_sort

            case SymbolType._unary_array:
                {
                    // array constructor - process each item
                    ArrayNode exprArray = (ArrayNode)expr;
                    for (int i = 0; i < exprArray.expressions.Count; ++i)
                    {
                        Node item = exprArray.expressions[i];
                        Node value = this.processAST(item);
                        this.pushAncestry(exprArray, value);
                        exprArray.expressions[i] = value;
                    }
                    result = exprArray;
                }
                break; // _unary_array
            case SymbolType._unary_minus:
                {
                    UnaryMinusNode exprMinus = (UnaryMinusNode)expr;
                    Node expression = this.processAST(exprMinus.expression);

                    // if unary minus on a number, then pre-process
                    if (expression.type == SymbolType._number_double)
                    {
                        result = new NumberDoubleNode(expression.position, -((NumberDoubleNode)expression).value);
                    }
                    else if (expression.type == SymbolType._number_int)
                    {
                        result = new NumberIntNode(expression.position, -((NumberIntNode)expression).value);
                    }
                    else
                    {
                        result = new UnaryMinusNode(expr.position, expression);
                        this.pushAncestry(result, expression);
                    }
                }
                break; // _unary_minus
            case SymbolType._unary_group:
                {
                    // object constructor - process each pair
                    GroupNode exprGroup = (GroupNode)expr;
                    for (int i = 0; i < exprGroup.lhsObject.Count; ++i)
                    {
                        Tuple<Node, Node> oldPair = exprGroup.lhsObject[i];
                        Node key = this.processAST(oldPair.Item1);
                        this.pushAncestry(result, key);
                        Node value = this.processAST(oldPair.Item2);
                        this.pushAncestry(result, value);
                        Tuple<Node, Node> newPair = Tuple.Create(key, value);
                        exprGroup.lhsObject[i] = newPair;
                    }
                }
                break; //_unary_object
            case SymbolType.function:
            case SymbolType.partial:
                {
                    FunctionalNode functionalExpr = (FunctionalNode)expr;
                    Node procedure = processAST(functionalExpr.procedure);
                    List<Node> arguments = new();
                    result = new FunctionalNode(expr.type, expr.position, procedure: procedure, arguments: arguments);
                    foreach (Node arg in functionalExpr.arguments)
                    { 
                        Node argAST = this.processAST(arg);
                        this.pushAncestry(result, argAST);
                        arguments.Add(argAST);
                    }
                }
                break;
            case SymbolType.lambda:
                {
                    LambdaNode lambdaExpr = (LambdaNode)expr;
                    Node body = this.processAST(lambdaExpr.body);
                    body = this.tailCallOptimize(body);
                    if (lambdaExpr.thunk)
                    {
                        throw new Exception("IF this may happen then why not pass `lambdaExpr.thunk` as a `result.thunk`?");
                    }
                    result = new LambdaNode(expr.position, arguments: lambdaExpr.arguments, signature: lambdaExpr.signature, body: body, thunk: false);
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
                    BlockNode exprBlock = (BlockNode)expr;
                    for (int i = 0; i < exprBlock.expressions.Count; ++i)
                    {
                        Node item = exprBlock.expressions[i];
                        Node part = this.processAST(item);
                        this.pushAncestry(exprBlock, part);
                        if (part.consarray || (part.type == SymbolType.path && ((PathNode)part).steps[0].consarray))
                        {
                            exprBlock.consarray = true;
                        }
                        exprBlock.expressions[i] = part;
                    }
                    result = exprBlock;
                    // TODO scan the array of expressions to see if any of them assign variables
                    // if so, need to mark the block as one that needs to create a new frame
                }
                break;
            case SymbolType.name:
                {
                    result = new PathNode(new() { (NameNode)expr }) {
                        keepSingletonArray = expr.keepArray
                    };
                }
                break;
            case SymbolType._parent: //this one is parsed from Parser
                {
                    SlotNode slot = new SlotNode(label: "!" + this.ancestorLabel++, index_int: this.ancestorIndex++, level: 1);
                    ParentWithSlotNode slottedResult = new ParentWithSlotNode(expr.position, slot);
                    this.ancestry.Add(slottedResult);
                    result = slottedResult;
                }
                break;
            case SymbolType.parent:
                throw new Exception("Should not happen!");  //if does happen, then probably repeat the stuff above
            case SymbolType.@string:
            case SymbolType._number_double:
            case SymbolType._number_int:
            case SymbolType._value_bool:
            case SymbolType._value_null:
            case SymbolType.wildcard:
            case SymbolType.descendant:
            case SymbolType.variable:
            case SymbolType.regex:
                result = expr;
                break;
            case SymbolType.@operator:
                {
                    OperatorNode exprOperator = (OperatorNode)expr;
                    // the tokens 'and' and 'or' might have been used as a name rather than an operator
                    switch (exprOperator.value)
                    {
                    case OperatorType.and:
                        result = this.processAST(new NameNode(expr.position, "and"));
                        break;
                    case OperatorType.or:
                        result = this.processAST(new NameNode(expr.position, "or"));
                        break;
                    case OperatorType.@in:
                        result = this.processAST(new NameNode(expr.position, "in"));
                        break;
                    case OperatorType.partial:
                        // partial application
                        result = expr;
                        break;
                    default:
                        throw new JException("S0201", expr.position/*, expr.value*/); //TODO: value
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
                    throw new JException(code, expr.position/*, expr.value*/); //TODO: value
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

        private static readonly Dictionary<string, NodeFactoryBase> s_binaryFactoryTable = CreateNodeFactoryTable();
        internal static readonly NodeFactoryBase s_terminalFactoryEnd = new TerminalFactoryTyped(SymbolType._end);
        internal static readonly NodeFactoryBase s_terminalFactoryName = new TerminalFactoryName();
        internal static readonly NodeFactoryBase s_terminalFactoryVariable = new TerminalFactoryVariable();
        internal static readonly NodeFactoryBase s_terminalFactoryNumberDouble = new TerminalFactoryNumberDouble();
        internal static readonly NodeFactoryBase s_terminalFactoryNumberInt = new TerminalFactoryNumberInt();
        internal static readonly NodeFactoryBase s_terminalFactoryString = new TerminalFactoryString();
        internal static readonly NodeFactoryBase s_terminalFactoryValueBool = new TerminalFactoryValueBool();
        internal static readonly NodeFactoryBase s_terminalFactoryValueNull = new TerminalFactoryValueNull();
        internal static readonly NodeFactoryBase s_terminalFactoryRegex = new TerminalFactoryRegex();

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
            register(nodeFactoryTable, new InfixAndPrefixMinusFactory("-")); // numeric subtraction // unary numeric negation

            register(nodeFactoryTable, new InfixWildcardFactory("*")); // field wildcard (single level) // numeric multiplication
            register(nodeFactoryTable, new InfixFactory("/")); // numeric division
            register(nodeFactoryTable, new InfixParentFactory("%")); // parent operator // numeric modulus
            register(nodeFactoryTable, new InfixFactory("=")); // equality
            register(nodeFactoryTable, new InfixFactory("<")); // less than
            register(nodeFactoryTable, new InfixFactory(">")); // greater than
            register(nodeFactoryTable, new InfixFactory("!=")); // not equal to
            register(nodeFactoryTable, new InfixFactory("<=")); // less than or equal
            register(nodeFactoryTable, new InfixFactory(">=")); // greater than or equal
            register(nodeFactoryTable, new InfixFactory("&")); // string concatenation

            register(nodeFactoryTable, new InfixWithOperatorPrefixFactory("and", OperatorType.and)); // allow as terminal // Boolean AND
            register(nodeFactoryTable, new InfixWithOperatorPrefixFactory("or", OperatorType.or)); // allow as terminal // Boolean OR
            register(nodeFactoryTable, new InfixWithOperatorPrefixFactory("in", OperatorType.@in)); // allow as terminal // is member of array
            register(nodeFactoryTable, new InfixFactory("~>")); // function application
            register(nodeFactoryTable, new InfixCoalescingFactory("??"));   // coalescing operator
            register(nodeFactoryTable, new PrefixDescendantWindcardFactory("**")); // descendant wildcard (multi-level)
            register(nodeFactoryTable, new InfixInvocationFactory("(")); // function invocation
            register(nodeFactoryTable, new InfixArrayFactory("[")); // array constructor // merged: register(new Prefix("[") {        
            register(nodeFactoryTable, new InfixOrderByFactory("^")); // order-by
            register(nodeFactoryTable, new InfixBlockFactory("{"));
            register(nodeFactoryTable, new InfixBindAssignVarFactory(":=")); // bind variable
            register(nodeFactoryTable, new InfixBindContextVarFactory("@")); // focus variable bind
            register(nodeFactoryTable, new InfixBindPositionalVarFactory("#")); // index (position) variable bind
            register(nodeFactoryTable, new InfixTernaryFactory("?")); // if/then/else ternary operator ?:
            register(nodeFactoryTable, new InfixElvisFactory("?:")); // elvis/default operator
            register(nodeFactoryTable, new PrefixTransformerFactory("|")); // object transformer
            return nodeFactoryTable;
        }
    }
}
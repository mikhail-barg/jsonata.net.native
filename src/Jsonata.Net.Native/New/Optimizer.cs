using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jsonata.Net.Native.New
{
    // was part of Parser
    internal sealed class Optimizer
    {
        internal static Node OptimizeAst(Node root)
        {
            Optimizer optimizer = new Optimizer();

            Node result = optimizer.processAST(root);

            if (result.type == SymbolType.parent || result.seekingParent != null)
            {
                // error - trying to derive ancestor at top level
                throw new JException("S0217", result.position, result.type);
            }

            return result;
        }


        int ancestorLabel = 0;
        int ancestorIndex = 0;
        List<ParentOptimizedNode> ancestry = new();

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
                        this.ancestry[slot.ancestorIndex].slot.label = node.ancestor.label;
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
                    PathRuntimeNode pathNode = (PathRuntimeNode)node;
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
                    slots.Add(((ParentOptimizedNode)value).slot);
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

        private void resolveAncestry(PathRuntimeNode path)
        {
            int index = path.steps.Count - 1;
            Node laststep = path.steps[index];
            List<SlotNode> slots = laststep.seekingParent ?? new();
            if (laststep.type == SymbolType.parent)
            {
                slots.Add(((ParentOptimizedNode)laststep).slot);
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
            case SymbolType._binary_path_node:
                {
                    PathConstructionNode exprBinaryPath = (PathConstructionNode)expr;
                    PathRuntimeNode resultPath;
                    Node lstep = this.processAST(exprBinaryPath.lhs);
                    if (lstep.type == SymbolType.path)
                    {
                        resultPath = (PathRuntimeNode)lstep;
                    }
                    else
                    {
                        resultPath = new PathRuntimeNode(new() { lstep });
                    }
                    if (lstep.type == SymbolType.parent)
                    {
                        resultPath.seekingParent = new() { ((ParentOptimizedNode)lstep).slot };
                    }
                    Node rest = this.processAST(exprBinaryPath.rhs);
                    if (rest.type == SymbolType.path)
                    {
                        resultPath.steps.AddRange(((PathRuntimeNode)rest).steps);
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
                break; //_binary_path_node
            case SymbolType._binary_filter_node:
                {
                    FilterConstructionNode exprBinaryFilter = (FilterConstructionNode)expr;
                    // predicated step
                    // LHS is a step or a predicated step
                    // RHS is the predicate expr
                    result = this.processAST(exprBinaryFilter.lhs);
                    Node step = result;
                    bool typeIsStages;      //if false -> type is 'predicate'
                    if (result.type == SymbolType.path)
                    {
                        step = ((PathRuntimeNode)result).steps[^1];
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

                    Node predicate = this.processAST(exprBinaryFilter.rhs);
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
                break; //_binary_filter_node
            case SymbolType.apply: // was part of binary
                {
                    ApplyNode exprApply = (ApplyNode)expr;
                    Node lhs = this.processAST(exprApply.lhs);
                    Node rhs = this.processAST(exprApply.rhs);
                    result = new ApplyNode(exprApply.position, lhs, rhs);
                    result.keepArray = lhs.keepArray || rhs.keepArray;
                }
                break; // apply
            case SymbolType.binary:
                {
                    BinaryNode exprBinary = (BinaryNode)expr;
                    Node lhs = this.processAST(exprBinary.lhs);
                    Node rhs = this.processAST(exprBinary.rhs);
                    result = new BinaryNode(expr.position, exprBinary.value, lhs, rhs);
                    this.pushAncestry(result, lhs);
                    this.pushAncestry(result, rhs);
                }
                break; // binary
            case SymbolType._binary_bind_assign:
                {
                    BindAssignVarConstructionNode exprBind = (BindAssignVarConstructionNode)expr;
                    Node lhs = this.processAST(exprBind.lhs);
                    Node rhs = this.processAST(exprBind.rhs);
                    if (lhs.type != SymbolType.variable)
                    {
                        throw new Exception("Should not happen, because exprBind.lhs was variable!");
                    }
                    result = new BindRuntimeNode(exprBind.position, (VariableNode)lhs, rhs);
                    this.pushAncestry(result, rhs);
                }
                break;
            case SymbolType._binary_bind_context:
                {
                    BindContextVarConstructionNode exprBind = (BindContextVarConstructionNode)expr;
                    result = this.processAST(exprBind.lhs);
                    Node step = result;
                    if (result.type == SymbolType.path)
                    {
                        step = ((PathRuntimeNode)result).steps[^1];
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
                    BindPositionalVarConstructionNode exprBind = (BindPositionalVarConstructionNode)expr;
                    result = processAST(exprBind.lhs);
                    Node step;
                    if (result.type == SymbolType.path)
                    {
                        step = ((PathRuntimeNode)result).steps[^1];
                    }
                    else
                    {
                        step = result;
                        result = new PathRuntimeNode(new() { step });
                        if (step.predicate != null)
                        {
                            step.stages = step.predicate.OfType<StageNode>().ToList();
                            step.predicate = null;
                        }
                    }
                    if (step.stages == null)
                    {
                        step.index = exprBind.rhs.value;
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
                    GroupByConstructionNode exprGroupby = (GroupByConstructionNode)expr;
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
                    OrderbyConstructionNode exprOrderby = (OrderbyConstructionNode)expr;
                    Node res = this.processAST(exprOrderby.lhs);
                    PathRuntimeNode resultPath;
                    if (res.type == SymbolType.path)
                    {
                        resultPath = (PathRuntimeNode)res;
                    }
                    else
                    {
                        resultPath = new PathRuntimeNode(new() { res });
                    }
                    SortStepNode sortStep = new SortStepNode(expr.position, terms: new());
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
                    result = new FunctionalNode(functionalExpr.type == SymbolType.partial, expr.position, procedure: procedure, arguments: arguments);
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
                        if (part.consarray || (part.type == SymbolType.path && ((PathRuntimeNode)part).steps[0].consarray))
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
                    result = new PathRuntimeNode(new() { (NameNode)expr }) {
                        keepSingletonArray = expr.keepArray
                    };
                }
                break;
            case SymbolType._parent: //this one is parsed from Parser
                {
                    SlotNode slot = new SlotNode(label: "!" + this.ancestorLabel++, ancestorIndex: this.ancestorIndex++, level: 1);
                    ParentOptimizedNode slottedResult = new ParentOptimizedNode(expr.position, slot);
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
                    SpecialOperatorNode exprOperator = (SpecialOperatorNode)expr;
                    // the tokens 'and' and 'or' might have been used as a name rather than an operator
                    switch (exprOperator.value)
                    {
                    case SpecialOperatorType.and:
                        result = this.processAST(new NameNode(expr.position, "and"));
                        break;
                    case SpecialOperatorType.or:
                        result = this.processAST(new NameNode(expr.position, "or"));
                        break;
                    case SpecialOperatorType.@in:
                        result = this.processAST(new NameNode(expr.position, "in"));
                        break;
                    case SpecialOperatorType.partial:
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

    }
}

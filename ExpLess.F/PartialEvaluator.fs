module public ExpLess.PartialEvaluator 

open System
open System.Linq.Expressions
open System.Reflection
open Breakdown


let rec public PreEvaluate (E : Expression) = 
    match categorize E with
    
    | Quote op -> match PreEvaluate op with
                  | same_op when same_op = op -> E
                  | new_op -> upcast Expression.Quote(new_op)
    
    | Lambda L -> match PreEvaluate L.Body with
                  | samebody when samebody = L.Body -> E
                  | new_body -> upcast Expression.Lambda(new_body, L.Parameters)

    | Binary (left, right, op) ->
            let new_left = PreEvaluate left
            let new_right = PreEvaluate right
            match new_left :? ConstantExpression, new_right :? ConstantExpression, op with
            | true, true, _ -> upcast Expression.Constant(Expression.Lambda(Expression.MakeBinary(E.NodeType, new_left, new_right)).Compile().DynamicInvoke(null), E.Type)
            | true, _, ShortLogic AndAlso -> match (new_left :?> ConstantExpression).Value :?> bool with
                                             | false -> upcast Expression.Constant(false)
                                             | true  -> new_right 
            | _, true, ShortLogic AndAlso -> match (new_right :?> ConstantExpression).Value :?> bool with
                                             | false -> upcast Expression.Constant(false)
                                             | true  -> new_left  
            | true, _, ShortLogic OrElse -> match (new_left :?> ConstantExpression).Value :?> bool with
                                             | true -> upcast Expression.Constant(true)
                                             | false  -> new_right 
            | _, true, ShortLogic OrElse -> match (new_right :?> ConstantExpression).Value :?> bool with
                                             | true -> upcast Expression.Constant(true)
                                             | false  -> new_left            
            | _ when new_left = left && new_right = right -> E
            | _ -> upcast Expression.MakeBinary(E.NodeType, new_left, new_right)
             
    | ParamAccess _ -> E

    | ConstAccess (exp, mem) -> match mem with
                                | :? FieldInfo -> upcast Expression.Constant((mem :?> FieldInfo).GetValue(exp.Value))
                                | _ -> raise (new NotImplementedException ("Constant member access is not implemented for this type of member: " + mem.GetType().Name))
    
    | Unary (op, operand) -> match PreEvaluate operand with
                             | new_op when (new_op :? ConstantExpression) -> upcast Expression.Constant(Expression.Lambda(Expression.MakeUnary(E.NodeType, new_op, E.Type)).Compile().DynamicInvoke(null), E.Type)
                             | same_op when same_op = operand -> E
                             | new_op -> upcast Expression.MakeUnary(E.NodeType, new_op, E.Type)

    | Conditional (test, iftrue, iffalse) -> 
            match PreEvaluate test with
            | const_test when (const_test :? ConstantExpression) -> 
                    match (const_test :?> ConstantExpression).Value :?> bool with
                    | true  -> PreEvaluate iftrue
                    | false -> PreEvaluate iffalse
            | new_test -> match PreEvaluate iftrue, PreEvaluate iffalse with
                          | same_true, same_false when new_test = test && same_false = iffalse && same_true = iftrue -> E
                          | new_true, new_false -> upcast Expression.Condition(new_test, new_true, new_false)

    | TypeIs (exp, t) -> match PreEvaluate exp with
                         | const_exp when (const_exp :? ConstantExpression) ->
                                upcast Expression.Constant(Expression.Lambda(Expression.TypeIs(const_exp, t)).Compile().DynamicInvoke(null), E.Type)
                         | same_exp when same_exp = exp -> E
                         | new_exp -> upcast Expression.TypeIs(new_exp, t)         
    
    | Call (obj, met, args) -> let HasSideEffects = false
                               if HasSideEffects then raise(new InvalidOperationException("method '" + met.Name + "' is not proved to be pure"))
                               else let new_args = List.map PreEvaluate args
                                    let new_object  = PreEvaluate obj
                                    let new_callexpr = Expression.Call(new_object, met, new_args)
                                    
                                    if (new_object :? ConstantExpression && List.fold (fun all (elem : Expression) -> all && elem :? ConstantExpression) true new_args)
                                    then upcast Expression.Constant(Expression.Lambda(new_callexpr).Compile().DynamicInvoke(null), E.Type) 
                                    else upcast new_callexpr                       
                       
    | Unsupported hint -> raise(new NotImplementedException ( "ExpLess::PreEvaluate - expressions like '" + E.ToString() + "' are not supported. Hint: " + hint + ".")) 
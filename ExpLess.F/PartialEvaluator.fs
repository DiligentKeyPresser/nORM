﻿module public ExpLess.PartialEvaluator 

open System
open System.Linq.Expressions
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
            | _, _, _ when new_left = left && new_right = right -> E
            | _ -> upcast Expression.MakeBinary(E.NodeType, new_left, new_right)
             

                      
    | Unsupported reason -> raise(new NotImplementedException ( "ExpLess::PreEvaluate - expressions like '" + E.ToString() + "' are not supported. Hint: " + reason + ".")) 
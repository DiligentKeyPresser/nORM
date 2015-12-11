module public ExpLess.PartialEvaluator 

open System
open System.Linq.Expressions
open System.Reflection
open Breakdown
open Purity

// unary operator 'partial evaluation'

let rec internal (~~~~) (tree: ExpressionNode) = 
    match tree with
    | Quote op                 -> Quote  (~~~~ op)
    | Lambda (body, args)      -> Lambda (~~~~ body, args)
    
    | Binary (left, op, right) -> 
            match ~~~~ left, ~~~~ right with
            | Constant (left, PaItself), Constant(right, PaItself) ->
                match op with
                | Shift Left                   -> Constant( downcast left <<< downcast right , PaItself )
                | Shift Right                  -> Constant( downcast left >>> downcast right , PaItself )
                | Compare LessThan             -> Constant( downcast left  <  downcast right , PaItself )
                | Compare LessThanOrEqual      -> Constant( downcast left  <= downcast right , PaItself )
                | Compare GreaterThan          -> Constant( downcast left  >  downcast right , PaItself )
                | Compare GreaterThanOrEqual   -> Constant( downcast left  >= downcast right , PaItself )
                | Compare Equal                -> Constant( downcast left  =  downcast right , PaItself )
                | Compare NotEqual             -> Constant( downcast left <>  downcast right , PaItself )
                | Math Add                     -> Constant( downcast left  +  downcast right , PaItself )
                | Math Subtract                -> Constant( downcast left  -  downcast right , PaItself )
                | Math Multiply                -> Constant( downcast left  *  downcast right , PaItself )
                | Math Divide                  -> Constant( downcast left  /  downcast right , PaItself )
                | Math Modulo                  -> Constant( downcast left  %  downcast right , PaItself )
                | Logic Or                     -> Constant( downcast left ||| downcast right , PaItself )
                | Logic And                    -> Constant( downcast left &&& downcast right , PaItself )
                | Logic ExclusiveOr            -> Constant( downcast left ^^^ downcast right , PaItself )
                | Logic (ShortCircuit AndAlso) -> Constant( downcast left  && downcast right , PaItself )
                | Logic (ShortCircuit OrElse)  -> Constant( downcast left  || downcast right , PaItself )
                | ArrayIndex                   -> Constant( Expression.Lambda(Expression.ArrayIndex(Expression.Constant(left), Expression.Constant(right))).Compile().DynamicInvoke(null) , PaItself )
                | Coalesce                     -> Constant( Expression.Lambda(Expression.Coalesce(Expression.Constant(left), Expression.Constant(right))).Compile().DynamicInvoke(null) , PaItself )                                         
        
            | Constant (left, PaItself), R  when op = Logic (ShortCircuit AndAlso) -> if left :?> bool  then R else Constant (false, PaItself)
            | L, Constant (right, PaItself) when op = Logic (ShortCircuit AndAlso) -> if right :?> bool then L else Constant (false, PaItself)
            | Constant (left, PaItself), R  when op = Logic (ShortCircuit OrElse)  -> if left :?> bool  then Constant (true, PaItself) else R
            | L, Constant (right, PaItself) when op = Logic (ShortCircuit OrElse)  -> if right :?> bool then Constant (true, PaItself) else L
            | L, R -> Binary (L, op, R)

    | Param _                  -> tree 
    | Constant (_, PaItself)   -> tree
    | Constant (e, PaMember m) -> 
            match m with  // can actually be only FieldInfo or PropertyInfo
            | :? FieldInfo    as F -> Constant (F.GetValue(e), PaItself)
            | :? PropertyInfo as P -> Constant (P.GetValue(e), PaItself)

    | Unary (op, exp) ->
            match ~~~~ exp with
            | Constant (new_operand, PaItself) -> 
                match op with
                | ArrayLength      -> Constant ( Expression.Lambda(Expression.ArrayLength(Expression.Constant(new_operand))).Compile().DynamicInvoke(null) , PaItself)
                | Checked ChNegate -> Constant ( Expression.Lambda(Expression.NegateChecked(Expression.Constant(new_operand))).Compile().DynamicInvoke(null) , PaItself)
                | Negate           -> Constant ( - (downcast new_operand), PaItself)
                | Not              -> Constant ( Expression.Lambda(Expression.Not(Expression.Constant(new_operand))).Compile().DynamicInvoke(null) , PaItself)
                | OnesComplement   -> Constant ( ~~~ (downcast new_operand), PaItself)
                | UnaryPlus        -> Constant ( + (downcast new_operand), PaItself)
            | A -> Unary (op, A)

    | Call (o, met, a) ->
            let HasSideEffects = not (IsPure met) 
            if HasSideEffects then raise(new InvalidOperationException("method '" + met.Name + "' is not proved to be pure")) 
            else let new_args = Seq.map (~~~~) a
                 let new_object = match o with
                                  | Constant (null, PaItself) -> o
                                  | _ -> ~~~~o
                 match new_object with
                 | Constant _ -> let args_are_const = Seq.fold (fun all (elem : ExpressionNode) -> all && match elem with | Constant _ -> true | _ -> false ) true new_args
                                 if args_are_const then Constant (Expression.Lambda(Expression.Call(inflate new_object, met, Seq.map inflate new_args)).Compile().DynamicInvoke(null), PaItself) 
                                 else Call (new_object, met, new_args) 
                 | _          -> Call (new_object, met, new_args)
                 
                 
                  
  
    | Unsupported hint -> raise(new NotImplementedException ( "ExpLess::PreEvaluate - expressions like '" + E.ToString() + "' are not supported. Hint: " + hint + ".")) 


let rec public PreEvaluate (E : Expression) = 
    match categorize E with
    

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
    

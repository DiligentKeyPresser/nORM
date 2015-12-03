module private ExpLess.Breakdown

open System.Linq.Expressions

// This module performs an internal expression classification.

// Methods and types are not public because this classification is affected by 
// the current domain (linq provider designing) heavily and tend to be useless outside.

type internal LogicOp = Or | And | ExclusiveOr

type internal ShiftOp = Left | Right

type internal CompareOp = LessThan | LessThanOrEqual | GreaterThan | GreaterThanOrEqual | Equal | NotEqual

type internal MathOp = Add | Subtract | Multiply | Divide | Modulo 

type internal ShortCircuitLogicOp = AndAlso | OrElse

type internal BinaryExpressionKind =
    | Shift      of ShiftOp   
    | Compare    of CompareOp 
    | Math       of MathOp
    | Logic      of LogicOp
    | ShortLogic of ShortCircuitLogicOp
    | Coalesce
    | ArrayIndex

type internal ExpressionKind =
    | Quote  of Expression           // unary expression, ExpressionType.Quote 
    | Lambda of LambdaExpression     // lambda expression
    | Binary of left : Expression * right : Expression * BinaryExpressionKind
    | Unsupported of string          // something else

let inline internal categorize (E : Expression) = 
    match E.NodeType with
    
    // Every lambda is wrapped into Quote
    | ExpressionType.Quote -> let e_unary = E :?> UnaryExpression
                              match e_unary.Method, e_unary.IsLifted, e_unary.IsLiftedToNull with
                              | null, false, false -> Quote e_unary.Operand
                              | _                  -> Unsupported "invalid quote"
    
    // Lambda itself
    | ExpressionType.Lambda -> Lambda (E :?> LambdaExpression)

    // Binary operators
    | ExpressionType.Coalesce | ExpressionType.ArrayIndex | ExpressionType.LeftShift | ExpressionType.RightShift | ExpressionType.Add| ExpressionType.Subtract |
      ExpressionType.Multiply | ExpressionType.Divide | ExpressionType.Modulo | ExpressionType.LessThan | ExpressionType.LessThanOrEqual | 
      ExpressionType.GreaterThan | ExpressionType.GreaterThanOrEqual | ExpressionType.AndAlso | ExpressionType.OrElse | ExpressionType.Equal |
      ExpressionType.NotEqual | ExpressionType.Or | ExpressionType.And | ExpressionType.ExclusiveOr -> 
            let e_binary = E :?> BinaryExpression
            let op_kind = match e_binary.NodeType with
                          | ExpressionType.Coalesce           -> Coalesce
                          | ExpressionType.ArrayIndex         -> ArrayIndex
                          | ExpressionType.LeftShift          -> Shift Left
                          | ExpressionType.RightShift         -> Shift Right
                          | ExpressionType.LessThan           -> Compare LessThan
                          | ExpressionType.LessThanOrEqual    -> Compare LessThanOrEqual
                          | ExpressionType.GreaterThan        -> Compare GreaterThan
                          | ExpressionType.GreaterThanOrEqual -> Compare GreaterThanOrEqual
                          | ExpressionType.Equal              -> Compare Equal
                          | ExpressionType.NotEqual           -> Compare NotEqual
                          | ExpressionType.Add                -> Math Add
                          | ExpressionType.Subtract           -> Math Subtract
                          | ExpressionType.Multiply           -> Math Multiply
                          | ExpressionType.Divide             -> Math Divide
                          | ExpressionType.Modulo             -> Math Modulo
                          | ExpressionType.AndAlso            -> ShortLogic AndAlso
                          | ExpressionType.OrElse             -> ShortLogic OrElse
                          | ExpressionType.Or                 -> Logic Or
                          | ExpressionType.And                -> Logic And
                          | ExpressionType.ExclusiveOr        -> Logic ExclusiveOr
                          | _ -> raise (new System.NotImplementedException("Someone forgot about " + e_binary.NodeType.ToString() + " binary operator."))
            
            match e_binary.Conversion, e_binary.Method, e_binary.IsLifted, e_binary.IsLiftedToNull with
            | null, null, false, false -> Binary (e_binary.Left, e_binary.Right, op_kind)
            | _ -> Unsupported "this kind of binary operator is not implemented yet" 
      
    | ExpressionType.Power | ExpressionType.AddChecked | ExpressionType.MultiplyChecked | ExpressionType.SubtractChecked ->
            Unsupported ("this binary operator (" + E.NodeType.ToString() + ") is not implemented yet")
    
    // Something new
    | _ -> Unsupported "unexpected ExpressionType"
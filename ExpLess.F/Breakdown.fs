module private ExpLess.Breakdown

open System
open System.Linq.Expressions
open System.Reflection

// This module performs an internal expression classification.

// Methods and types are not public because this classification is affected by 
// the current domain (linq provider designing) heavily and tends to be useless outside.

type internal LogicOp = Or | And | ExclusiveOr
type internal ShiftOp = Left | Right
type internal CompareOp = LessThan | LessThanOrEqual | GreaterThan | GreaterThanOrEqual | Equal | NotEqual
type internal MathOp = Add | Subtract | Multiply | Divide | Modulo 
type internal ShortCircuitLogicOp = AndAlso | OrElse
type internal UnaryOp = Convert | TypeAs | ArrayLength | Negate | Not 

type internal BinaryExpressionKind =
    | Shift      of ShiftOp   
    | Compare    of CompareOp 
    | Math       of MathOp
    | Logic      of LogicOp
    | ShortLogic of ShortCircuitLogicOp
    | Coalesce
    | ArrayIndex

type internal ExpressionKind =
    | Quote  of Expression                            // unary expression, ExpressionType.Quote 
    | Lambda of LambdaExpression                      // lambda expression
    | TypeIs of Expression * Type                     // 'is' operator
    | Binary of left : Expression * right : Expression * BinaryExpressionKind
    | ParamAccess of ParameterExpression              // member access expression, parameter
    | ConstAccess of ConstantExpression * MemberInfo  // member access expression, constant
    | Unary of UnaryOp * Expression                   // unary operator
    | Conditional of test : Expression * iftrue : Expression * iffalse : Expression
    | Call of obj : Expression * met : MethodInfo * args : Expression list
    | Unsupported of string                           // something else

let internal categorize (E : Expression) = 
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
    
    // Lambda parameter or constant value access
    | ExpressionType.MemberAccess -> let e_member = E :?> MemberExpression
                                     match e_member.Expression.NodeType with
                                     | ExpressionType.Parameter -> match e_member.Member with
                                                                   | null -> ParamAccess (e_member.Expression :?> ParameterExpression)
                                                                   | _ -> Unsupported "parameter member access is not implemented yet"
                                     | ExpressionType.Constant  -> ConstAccess (e_member.Expression :?> ConstantExpression, e_member.Member)
                                     | _ -> Unsupported ("Member access for '" + e_member.Expression.NodeType.ToString() + "' expression is not implemented yet")

    // Unary operators
    | ExpressionType.Convert | ExpressionType.TypeAs | ExpressionType.ArrayLength | ExpressionType.Negate | ExpressionType.Not
        -> let e_unary = E :?> UnaryExpression
           match e_unary.Method, e_unary.IsLifted, e_unary.IsLiftedToNull with
           | null, false, false -> match E.NodeType with
                                   | ExpressionType.Convert     -> Unary (Convert, e_unary.Operand)
                                   | ExpressionType.TypeAs      -> Unary (TypeAs, e_unary.Operand)
                                   | ExpressionType.ArrayLength -> Unary (ArrayLength, e_unary.Operand)
                                   | ExpressionType.Negate      -> Unary (Negate, e_unary.Operand)
                                   | ExpressionType.Not         -> Unary (Not, e_unary.Operand)
                                   | _ -> raise (new System.NotImplementedException("Someone forgot about " + E.NodeType.ToString() + " unary operator."))
           | _ -> Unsupported "this kind of unary operator is not implemented yet" 
           
    | ExpressionType.UnaryPlus // +...
    | ExpressionType.NegateChecked | ExpressionType.ConvertChecked
    | ExpressionType.OnesComplement // ~
        -> Unsupported ("unary operator '" + E.NodeType.ToString() + "' is not implemented yet")

    // Ternary operator ? :
    | ExpressionType.Conditional -> let e_cond = E :?> ConditionalExpression
                                    Conditional (e_cond.Test, e_cond.IfTrue, e_cond.IfFalse)

    // 'is' operator
    | ExpressionType.TypeIs -> let e_is = E :?> TypeBinaryExpression
                               TypeIs (e_is.Expression, e_is.TypeOperand)

    // Method call
    | ExpressionType.Call -> let e_call = E :?> MethodCallExpression
                             Call (e_call.Object, e_call.Method, List.ofSeq e_call.Arguments)
    




    // Delegate invocation                         
    | ExpressionType.Invoke -> Unsupported "Delegate invocation has not been implemented" 

    // NewArrayExpression was not useful anyway
    | ExpressionType.NewArrayInit | ExpressionType.NewArrayBounds 
            -> Unsupported ("expression '" + E.NodeType.ToString() + "' considered to be useless")

    // Something new
    | _ -> Unsupported "unexpected ExpressionType"
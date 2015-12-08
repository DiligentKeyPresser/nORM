module private ExpLess.Breakdown

open System
open System.Linq.Expressions
open System.Reflection

// This module performs an internal expression classification.

// Methods and types are not public because this classification is affected by 
// the current domain (linq provider designing) heavily and tends to be useless outside.

type internal ShortCircuitLogicOp = AndAlso | OrElse
type internal LogicOp             = Or | And | ExclusiveOr | ShortCircuit of ShortCircuitLogicOp

type internal ShiftOp             = Left | Right

type internal CompareOp           = LessThan | LessThanOrEqual | GreaterThan | GreaterThanOrEqual | Equal | NotEqual

type internal MathOp              = Add | Subtract | Multiply | Divide | Modulo 

type internal CheckedUnaryOp      = Negate | Convert
type internal UnaryOp             = Convert | TypeAs | ArrayLength | Negate | Not | UnaryPlus | OnesComplement | Checked of CheckedUnaryOp

type internal ParamAccessMode     = PaItself | PaMember of MemberInfo

type internal BinaryNode =
    | Shift      of ShiftOp   
    | Compare    of CompareOp 
    | Math       of MathOp
    | Logic      of LogicOp
    | Coalesce
    | ArrayIndex

type internal ExpressionNode =
    | Constant    of obj * ParamAccessMode
    | Param       of ParameterExpression * ParamAccessMode
    | Quote       of ExpressionNode 
    | Lambda      of ExpressionNode * seq<ParameterExpression>
    | TypeIs      of ExpressionNode * Type
    | Binary      of ExpressionNode * BinaryNode * ExpressionNode
    | Unary       of UnaryOp * ExpressionNode
    | Conditional of test : ExpressionNode * iftrue : ExpressionNode * iffalse : ExpressionNode
    | Call        of obj : ExpressionNode * met : MethodInfo * args : seq<ExpressionNode>
    | Unsupported of string

let rec internal discriminate (E : Expression) : ExpressionNode = 
    match E.NodeType with
    
    // Every lambda is wrapped into Quote
    | ExpressionType.Quote -> let e_unary = E :?> UnaryExpression
                              match e_unary.Method, e_unary.IsLifted, e_unary.IsLiftedToNull with
                              | null, false, false -> discriminate e_unary.Operand |> Quote
                              | _                  -> Unsupported "invalid quote"
    
    // Lambda itself
    | ExpressionType.Lambda -> let e_lambda = E :?> LambdaExpression
                               Lambda (discriminate e_lambda.Body, e_lambda.Parameters)

    // direct param access
    | ExpressionType.Parameter -> Param (E :?> ParameterExpression, PaItself)

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
                          | ExpressionType.AndAlso            -> Logic (ShortCircuit AndAlso)
                          | ExpressionType.OrElse             -> Logic (ShortCircuit OrElse)
                          | ExpressionType.Or                 -> Logic Or
                          | ExpressionType.And                -> Logic And
                          | ExpressionType.ExclusiveOr        -> Logic ExclusiveOr
                          | _ -> raise (new System.NotImplementedException("Someone forgot about " + e_binary.NodeType.ToString() + " binary operator."))
            
            match e_binary.Conversion, e_binary.Method, e_binary.IsLifted, e_binary.IsLiftedToNull with
            | null, null, false, false -> Binary (discriminate e_binary.Left, op_kind, discriminate e_binary.Right)
            | _ -> Unsupported "this kind of binary operator is not implemented yet" 
      
    | ExpressionType.Power | ExpressionType.AddChecked | ExpressionType.MultiplyChecked | ExpressionType.SubtractChecked ->
            Unsupported ("this binary operator (" + E.NodeType.ToString() + ") is not implemented yet")
    
    // Lambda parameter or constant value access
    | ExpressionType.MemberAccess -> let e_member = E :?> MemberExpression
                                     match e_member.Expression.NodeType with
                                     | ExpressionType.Parameter -> Param (e_member.Expression :?> ParameterExpression, PaMember e_member.Member)
                                     | ExpressionType.Constant  -> Constant ((e_member.Expression :?> ConstantExpression).Value, PaMember e_member.Member)
                                     | _ -> Unsupported ("Member access for '" + e_member.Expression.NodeType.ToString() + "' expression is not implemented yet")

    // Unary operators
    | ExpressionType.Convert | ExpressionType.TypeAs | ExpressionType.ArrayLength | ExpressionType.Negate | ExpressionType.Not | ExpressionType.UnaryPlus 
    | ExpressionType.OnesComplement | ExpressionType.NegateChecked | ExpressionType.ConvertChecked
        -> let e_unary = E :?> UnaryExpression
           match e_unary.Method, e_unary.IsLifted, e_unary.IsLiftedToNull with
           | null, false, false -> match E.NodeType with
                                   | ExpressionType.Convert        -> Unary (Convert, discriminate e_unary.Operand)
                                   | ExpressionType.TypeAs         -> Unary (TypeAs, discriminate e_unary.Operand)
                                   | ExpressionType.ArrayLength    -> Unary (ArrayLength, discriminate e_unary.Operand)
                                   | ExpressionType.Negate         -> Unary (Negate, discriminate e_unary.Operand)
                                   | ExpressionType.Not            -> Unary (Not, discriminate e_unary.Operand)
                                   | ExpressionType.UnaryPlus      -> Unary (UnaryPlus, discriminate e_unary.Operand)
                                   | ExpressionType.OnesComplement -> Unary (OnesComplement, discriminate e_unary.Operand)
                                   | ExpressionType.NegateChecked  -> Unary (Checked CheckedUnaryOp.Negate, discriminate e_unary.Operand)
                                   | ExpressionType.ConvertChecked -> Unary (Checked CheckedUnaryOp.Convert, discriminate e_unary.Operand)
                                   | _ -> raise (new System.NotImplementedException("Someone forgot about " + E.NodeType.ToString() + " unary operator."))
           | _ -> Unsupported "this kind of unary operator is not implemented yet" 
           
    // Ternary operator ? :
    | ExpressionType.Conditional -> let e_cond = E :?> ConditionalExpression
                                    Conditional (discriminate e_cond.Test, discriminate e_cond.IfTrue, discriminate e_cond.IfFalse)

    // 'is' operator
    | ExpressionType.TypeIs -> let e_is = E :?> TypeBinaryExpression
                               TypeIs (discriminate e_is.Expression, e_is.TypeOperand)

    // Method call
    | ExpressionType.Call -> let e_call = E :?> MethodCallExpression
                             Call (discriminate e_call.Object, e_call.Method, [| for a in e_call.Arguments -> discriminate a |])
    
    // Constant
    | ExpressionType.Constant -> Constant ((E :?> ConstantExpression).Value, PaItself)

    // Implement later
    | ExpressionType.Extension | ExpressionType.Unbox | ExpressionType.MemberInit | ExpressionType.ListInit | ExpressionType.Index 
            -> Unsupported ("Expression '" + E.NodeType.ToString() + "' will be implemented later.")
    
    // These operations cannot be found in lambda code
    | ExpressionType.Dynamic -> Unsupported "An expression tree may not contain a dynamic operation"
    
    | ExpressionType.RuntimeVariables | ExpressionType.Loop | ExpressionType.Label | ExpressionType.Goto | ExpressionType.Throw
    | ExpressionType.Switch | ExpressionType.Block | ExpressionType.Try
            -> Unsupported "A lambda expression with a statement body cannot be converted to an expression tree"

    | ExpressionType.AddAssignChecked | ExpressionType.MultiplyAssignChecked | ExpressionType.SubtractAssignChecked | ExpressionType.Assign
    | ExpressionType.AddAssign | ExpressionType.AndAssign | ExpressionType.DivideAssign | ExpressionType.ExclusiveOrAssign
    | ExpressionType.LeftShiftAssign | ExpressionType.ModuloAssign | ExpressionType.MultiplyAssign | ExpressionType.RightShiftAssign
    | ExpressionType.SubtractAssign | ExpressionType.OrAssign | ExpressionType.PowerAssign | ExpressionType.PreIncrementAssign
    | ExpressionType.PreDecrementAssign | ExpressionType.PostIncrementAssign | ExpressionType.PostDecrementAssign
            -> Unsupported "An expression tree may not contain an assignment operator"

    // Unable to produce next ones
    | ExpressionType.Default | ExpressionType.IsTrue | ExpressionType.IsFalse | ExpressionType.Increment
    | ExpressionType.Decrement | ExpressionType.DebugInfo | ExpressionType.TypeEqual
            -> Unsupported "unexpected ExpressionType"

    // new                        
    | ExpressionType.New -> Unsupported "Creating new objects has not been implemented" 
    
    // Delegate invocation                         
    | ExpressionType.Invoke -> Unsupported "Delegate invocation has not been implemented" 

    // NewArrayExpression was not useful anyway
    | ExpressionType.NewArrayInit | ExpressionType.NewArrayBounds 
            -> Unsupported ("expression '" + E.NodeType.ToString() + "' considered to be useless")

    // Something new
    | _ -> Unsupported "unexpected ExpressionType"
module public ExpLess.Breakdown

open System
open System.Linq.Expressions
open System.Reflection

// This module performs an internal expression classification.

// Methods and types are not public because this classification is affected by 
// the current domain (linq provider designing) heavily and tends to be useless outside.


// Types of expression tree:

type internal ShortCircuitLogicOp = AndAlso | OrElse
type internal LogicOp             = Or | And | ExclusiveOr | ShortCircuit of ShortCircuitLogicOp

type internal ShiftOp             = Left | Right

type internal CompareOp           = LessThan | LessThanOrEqual | GreaterThan | GreaterThanOrEqual | Equal | NotEqual

type internal MathOp              = Add | Subtract | Multiply | Divide | Modulo 

type internal CheckedUnaryOp      = ChNegate
type internal UnaryOp             = ArrayLength | Negate | Not | UnaryPlus | OnesComplement | Checked of CheckedUnaryOp

type internal ParamAccessMode     = PaItself | PaMember of MemberInfo

type internal ConversionMode      = CConvert | CConvertChecked | CTypeAs

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
    | Convert     of Type * ExpressionNode * ConversionMode
    | Unsupported of string

// Convertion from Expression to internal tree representation
// Some expressions are impossible to convert into this form

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
            | null, null, false, false                                                                           -> Binary (discriminate e_binary.Left, op_kind, discriminate e_binary.Right)
            | null, _, false, false when e_binary.Left.Type = typeof<Guid> && e_binary.Right.Type = typeof<Guid> -> Binary (discriminate e_binary.Left, op_kind, discriminate e_binary.Right)
                           
            | _ -> Unsupported "this kind of binary operator is not implemented yet" 
      
    | ExpressionType.Power | ExpressionType.AddChecked | ExpressionType.MultiplyChecked | ExpressionType.SubtractChecked ->
            Unsupported ("this binary operator (" + E.NodeType.ToString() + ") is not implemented yet")
    
    // Lambda parameter or constant value access
    | ExpressionType.MemberAccess -> 
            let e_member = E :?> MemberExpression

            // static member access
            if e_member.Expression = null then  
                match e_member.Member with
                | :? PropertyInfo as prop  -> Constant (prop.GetValue(null), PaItself) 
                | :? FieldInfo    as field -> Constant (field.GetValue(null), PaItself) 
                | _ -> Constant (null, PaMember e_member.Member)

            // nonstatic member access
            else match e_member.Expression.NodeType with 
                 | ExpressionType.Parameter    -> Param (e_member.Expression :?> ParameterExpression, PaMember e_member.Member)
                 | ExpressionType.Constant     -> match e_member.Member with
                                                  | :? PropertyInfo as prop  -> Constant (prop.GetValue((e_member.Expression :?> ConstantExpression).Value), PaItself) 
                                                  | :? FieldInfo    as field -> Constant (field.GetValue((e_member.Expression :?> ConstantExpression).Value), PaItself) 
                                                  | _ -> Constant ((e_member.Expression :?> ConstantExpression).Value, PaMember e_member.Member)
                 | ExpressionType.MemberAccess -> let mem = discriminate e_member.Expression
                                                  match mem with
                                                  | Constant (value, PaItself) -> 
                                                        match e_member.Member with
                                                        | :? PropertyInfo as prop  -> Constant (prop.GetValue(value), PaItself) 
                                                        | :? FieldInfo    as field -> Constant (field.GetValue(value), PaItself) 
                                                        | _ -> Constant (value, PaMember e_member.Member)
                                                  | _ -> Unsupported ("Member access for '" + e_member.Expression.NodeType.ToString() + "' expression is not implemented yet")
                  | _ -> Unsupported ("Member access for '" + e_member.Expression.NodeType.ToString() + "' expression is not implemented yet")

    // Conversion operations
    | ExpressionType.Convert        -> let e_unary = E :?> UnaryExpression
                                       Convert (e_unary.Type, discriminate e_unary.Operand, CConvert)
    | ExpressionType.ConvertChecked -> let e_unary = E :?> UnaryExpression
                                       Convert (e_unary.Type, discriminate e_unary.Operand, CConvertChecked)
    | ExpressionType.TypeAs         -> let e_unary = E :?> UnaryExpression
                                       Convert (e_unary.Type, discriminate e_unary.Operand, CTypeAs)

    // Unary operators
    | ExpressionType.ArrayLength | ExpressionType.Negate | ExpressionType.Not | ExpressionType.UnaryPlus 
    | ExpressionType.OnesComplement | ExpressionType.NegateChecked 
        -> let e_unary = E :?> UnaryExpression
           match e_unary.Method, e_unary.IsLifted, e_unary.IsLiftedToNull with
           | null, false, false -> match E.NodeType with
                                   | ExpressionType.ArrayLength    -> Unary (ArrayLength, discriminate e_unary.Operand)
                                   | ExpressionType.Negate         -> Unary (Negate, discriminate e_unary.Operand)
                                   | ExpressionType.Not            -> Unary (Not, discriminate e_unary.Operand)
                                   | ExpressionType.UnaryPlus      -> Unary (UnaryPlus, discriminate e_unary.Operand)
                                   | ExpressionType.OnesComplement -> Unary (OnesComplement, discriminate e_unary.Operand)
                                   | ExpressionType.NegateChecked  -> Unary (Checked ChNegate, discriminate e_unary.Operand)
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

// Reversive conversion from internal form into an Expression

let rec internal inflate (tree: ExpressionNode) : Expression =
    match tree with
    | Binary (left, op, right) -> 
        let exp = match op with
                  | Shift Left                   -> ExpressionType.LeftShift
                  | Shift Right                  -> ExpressionType.RightShift
                  | ArrayIndex                   -> ExpressionType.ArrayIndex
                  | Coalesce                     -> ExpressionType.Coalesce
                  | Compare LessThan             -> ExpressionType.LessThan
                  | Compare LessThanOrEqual      -> ExpressionType.LessThanOrEqual
                  | Compare Equal                -> ExpressionType.Equal
                  | Compare GreaterThan          -> ExpressionType.GreaterThan
                  | Compare GreaterThanOrEqual   -> ExpressionType.GreaterThanOrEqual
                  | Compare NotEqual             -> ExpressionType.NotEqual
                  | Logic And                    -> ExpressionType.And
                  | Logic ExclusiveOr            -> ExpressionType.ExclusiveOr
                  | Logic Or                     -> ExpressionType.Or
                  | Logic (ShortCircuit AndAlso) -> ExpressionType.AndAlso
                  | Logic (ShortCircuit OrElse)  -> ExpressionType.OrElse
                  | Math Add                     -> ExpressionType.Add
                  | Math Divide                  -> ExpressionType.Divide
                  | Math Modulo                  -> ExpressionType.Modulo
                  | Math Multiply                -> ExpressionType.Multiply
                  | Math Subtract                -> ExpressionType.Subtract
        upcast Expression.MakeBinary(exp, inflate left, inflate right)
    
    | Convert (typ, op, mode) -> 
            match mode with
            | CConvert        -> upcast Expression.Convert(inflate op, typ)
            | CConvertChecked -> upcast Expression.ConvertChecked(inflate op, typ)
            | CTypeAs         -> upcast Expression.TypeAs(inflate op, typ)
    
    | Unary (op, exp) -> 
            match op with
            | ArrayLength      -> upcast Expression.ArrayLength(inflate exp)
            | Negate           -> upcast Expression.Negate(inflate exp)
            | Not              -> upcast Expression.Not(inflate exp)
            | UnaryPlus        -> upcast Expression.UnaryPlus(inflate exp)
            | OnesComplement   -> upcast Expression.OnesComplement(inflate exp)
            | Checked ChNegate -> upcast Expression.NegateChecked(inflate exp)
            
    | Call (exp, met, args)        -> upcast Expression.Call(inflate exp, met, Seq.map inflate args)
    | Conditional (test, tr, f)    -> upcast Expression.Condition(inflate test, inflate tr, inflate f)
    | Constant (obj, PaItself)     -> upcast Expression.Constant(obj) 
    | Constant (obj, PaMember mem) -> upcast Expression.MakeMemberAccess(Expression.Constant(obj), mem)
    | Lambda (body, par)           -> upcast Expression.Lambda(inflate body, par)
    | Param (par, PaItself)        -> upcast par
    | Param (par, PaMember mem)    -> upcast Expression.MakeMemberAccess(par, mem)
    | Quote exp                    -> upcast Expression.Quote(inflate exp)
    | TypeIs (exp, typ)            -> upcast Expression.TypeIs(inflate exp, typ)
    | Unsupported reason  -> raise(new NotImplementedException ( "inflate: not supported. Hint: " + reason + ".")) 
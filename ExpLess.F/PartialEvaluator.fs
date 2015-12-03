module ExpLess.PartialEvaluator 

open System
open System.Linq.Expressions

let rec public PreEvaluate (E : Expression) = 
    match E.NodeType with
    | ExpressionType.Quote -> 
        let e_unary = E :?> UnaryExpression
        match e_unary.Method with
        | null -> match PreEvaluate e_unary.Operand with
                  | new_operand when new_operand = e_unary.Operand -> E
                  | new_operand -> Expression.MakeUnary(ExpressionType.Quote, new_operand, E.Type) :> Expression
        | _    -> raise(new NotImplementedException "ExpLess::PreEvaluate - Quote 'Method' is not implemented yet.") 
    | _ -> E
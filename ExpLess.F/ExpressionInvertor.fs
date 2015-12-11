module public ExpLess.ExpressionInverter

open System
open System.Linq.Expressions
open Breakdown

let rec internal invert (tree : ExpressionNode) : ExpressionNode = 
    match tree with    
    | Lambda (body, args)          -> Lambda(invert body, args)
    | Quote op                     -> Quote (invert op)
    | Unary (Not, exp)             -> exp
    | _                            ->
            let expr = inflate tree
            if expr.Type = typeof<bool> then Unary (Not, tree)
            else raise (new NotImplementedException ("Expression '" + expr.ToString() + "' cannot be inverted"))
module public ExpLess.ExpressionInverter

open System
open System.Linq.Expressions
open Breakdown

let rec public Invert (E : Expression) : Expression = 
    match categorize E with    
    | Lambda L                     -> upcast Expression.Lambda(Invert L.Body, L.Parameters) 
    | Quote op                     -> upcast Expression.MakeUnary(E.NodeType, Invert op, E.Type) 
    | Unary (Not, exp)             -> exp
    | _ when E.Type = typeof<bool> -> upcast Expression.MakeUnary(ExpressionType.Not, E, E.Type)
    | _                            -> raise (new NotImplementedException ("Expression '" + E.ToString() + "' cannot be inverted"))
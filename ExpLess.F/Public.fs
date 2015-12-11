namespace ExpLess

open Breakdown
open System.Linq.Expressions
open PartialEvaluator
open System

// Public object to hold an internal representation of expression trees

[<Sealed>]
type public DiscriminatedExpression(exp: obj) =
    let tree = match exp with
               | :? Expression as e      -> discriminate e
               | :? ExpressionNode as n  -> n
               | _                       -> raise ( new ArgumentException("Cannot convert '" + exp.GetType().Name + "' into an expression.")) 
    member this.Build = inflate tree 
    member this.PreEvaluate = DiscriminatedExpression ~~~~tree


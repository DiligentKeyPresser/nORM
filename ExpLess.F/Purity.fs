module private Purity

open System
open System.Reflection

// Here we store informations about all the functions conidered to be pure

let internal IsPure (M : MethodInfo) =
    match M.DeclaringType with
    | t when t = typeof<Math> -> true
    | _ -> false

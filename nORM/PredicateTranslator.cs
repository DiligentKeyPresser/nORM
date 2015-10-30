using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace nORM
{
    internal static class PredicateTranslator
    {
        public static string[] TranslatePredicate<RowContract>(Expression E)
        {
#warning добавить предварительное вычисление
#warning обрабатываются только предикаты с одним аргументом
            return ToSQL<RowContract>(E, null);
        }

        /// <summary>
        /// Builds array from two arays and one string between them
        /// </summary>
        private static string[] MakeBinary(string[] Left, string op, string[] Right)
        {
            var result = new string[Left.Length + 1 + Right.Length];
            Array.Copy(Left, result, Left.Length);
            result[Left.Length] = op;
            Array.Copy(Right, 0, result, Left.Length + 1, Right.Length);
            return result;
        }

        private static string[] ToSQL<RowContract>(Expression E, Expression Row)
        {
            var e_constant = E as ConstantExpression;
            if (e_constant != null)
            {
                if (e_constant.Type == TypeOf.String) return new string[] { (string)e_constant.Value };
                if (e_constant.Type == TypeOf.Bool) return new string[] { (bool)e_constant.Value ? "1 = 1 " : "1 = 0" };
                if (e_constant.Type == TypeOf.Int32) goto to_string;
                if (e_constant.Type == TypeOf.Int16) goto to_string;

#warning add debug output
                return null;
            to_string:
                return new string[] { e_constant.Value.ToString() };
            }

            var e_member = E as MemberExpression;
            if (e_member != null)
            {
                var RowPropertyAccess = e_member.Expression == Row;
                if (RowPropertyAccess)
                {
#if DEBUG
                    if (!Attribute.IsDefined(e_member.Member, TypeOf.FieldAttribute)) throw new InvalidContractException(typeof(RowContract), "Field name is not defined.");
#endif
#warning не самый быстрый способ. не закешировать ли?
                    return new string[] { (Attribute.GetCustomAttribute(e_member.Member, TypeOf.FieldAttribute) as FieldAttribute).ColumnName };
                }


#warning add debug output
                return null;
            }

            var e_binary = E as BinaryExpression;
            if (e_binary != null)
            {
#warning do lifted calls need any special processing?
#warning does 'method' need any special processing?

                var Left = ToSQL<RowContract>(e_binary.Left, Row);
                if (Left == null)
#warning add debug output
                    return null;
                var Right = ToSQL<RowContract>(e_binary.Right, Row);
                if (Right == null)
#warning add debug output
                    return null;

                switch (e_binary.NodeType)
                {
                    case ExpressionType.Equal: return MakeBinary(Left, " = ", Right);
                    case ExpressionType.GreaterThan: return MakeBinary(Left, " > ", Right);
                    case ExpressionType.GreaterThanOrEqual: return MakeBinary(Left, " >= ", Right);
                    case ExpressionType.LessThan: return MakeBinary(Left, " < ", Right);
                    case ExpressionType.LessThanOrEqual: return MakeBinary(Left, " <= ", Right);

                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                    case ExpressionType.ArrayLength:
                    case ExpressionType.ArrayIndex:
                    case ExpressionType.Call:
                    case ExpressionType.Coalesce:
                    case ExpressionType.Conditional:
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                    case ExpressionType.Divide:
                    case ExpressionType.ExclusiveOr:
                    case ExpressionType.Invoke:
                    case ExpressionType.Lambda:
                    case ExpressionType.LeftShift:
                    case ExpressionType.ListInit:
                    case ExpressionType.MemberAccess:
                    case ExpressionType.MemberInit:
                    case ExpressionType.Modulo:
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                    case ExpressionType.Negate:
                    case ExpressionType.UnaryPlus:
                    case ExpressionType.NegateChecked:
                    case ExpressionType.New:
                    case ExpressionType.NewArrayInit:
                    case ExpressionType.NewArrayBounds:
                    case ExpressionType.NotEqual:
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                    case ExpressionType.Parameter:
                    case ExpressionType.Power:
                    case ExpressionType.RightShift:
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                    case ExpressionType.TypeAs:
                    case ExpressionType.TypeIs:
                    case ExpressionType.Assign:
                    case ExpressionType.Block:
                    case ExpressionType.DebugInfo:
                    case ExpressionType.Decrement:
                    case ExpressionType.Dynamic:
                    case ExpressionType.Default:
                    case ExpressionType.Extension:
                    case ExpressionType.Goto:
                    case ExpressionType.Increment:
                    case ExpressionType.Index:
                    case ExpressionType.Label:
                    case ExpressionType.RuntimeVariables:
                    case ExpressionType.Loop:
                    case ExpressionType.Switch:
                    case ExpressionType.Throw:
                    case ExpressionType.Try:
                    case ExpressionType.Unbox:
                    case ExpressionType.AddAssign:
                    case ExpressionType.AndAssign:
                    case ExpressionType.DivideAssign:
                    case ExpressionType.ExclusiveOrAssign:
                    case ExpressionType.LeftShiftAssign:
                    case ExpressionType.ModuloAssign:
                    case ExpressionType.MultiplyAssign:
                    case ExpressionType.OrAssign:
                    case ExpressionType.PowerAssign:
                    case ExpressionType.RightShiftAssign:
                    case ExpressionType.SubtractAssign:
                    case ExpressionType.AddAssignChecked:
                    case ExpressionType.MultiplyAssignChecked:
                    case ExpressionType.SubtractAssignChecked:
                    case ExpressionType.PreIncrementAssign:
                    case ExpressionType.PreDecrementAssign:
                    case ExpressionType.PostIncrementAssign:
                    case ExpressionType.PostDecrementAssign:
                    case ExpressionType.TypeEqual:
                    case ExpressionType.OnesComplement:
                    case ExpressionType.IsTrue:
                    case ExpressionType.IsFalse:
#warning add debug output
                        return null;
                    
                    case ExpressionType.Constant:
                    default:
                        throw new InvalidProgramException($"some unknown binary operator has been passed: {e_binary.NodeType.ToString()}");
                }
            }

            var e_lambda = E as LambdaExpression;
            if (e_lambda != null) return ToSQL<RowContract>(e_lambda.Body, e_lambda.Parameters[0]);

            var e_unary = E as UnaryExpression;
            if (e_unary != null)
            {
                switch (e_unary.NodeType)
                {
                    case ExpressionType.Not:
                        var positive = ToSQL<RowContract>(e_unary.Operand, Row);
                        var negative = new string[positive.Length + 2];
                        negative[0] = "NOT(";
                        Array.Copy(positive, 0, negative, 1, positive.Length);
                        negative[negative.Length - 1] = ") ";
                        return negative;
                    case ExpressionType.Quote: return ToSQL<RowContract>(e_unary.Operand, null);
                    default:
#warning add debug output
                        return null;
                }


            }
#warning add debug output
            return null;
        }

        /// <summary>
        /// Returns predicate which returns inverse result of the original one.
        /// </summary>
        /// <param name="E"> Original predicate. </param>
        public static Expression InvertPredicate(Expression E)
        {
#warning expression type must be checked
            var e_lambda = E as LambdaExpression;
            if (e_lambda != null)
            {
                var body = InvertPredicate(e_lambda.Body);
#warning add debug output
                if (body == null) return null;
                return Expression.Lambda(body, e_lambda.Parameters);
            }

            var e_unary = E as UnaryExpression;
            if (e_unary != null)
            {
                if (e_unary.NodeType == ExpressionType.Quote) {
                    var operand = InvertPredicate(e_unary.Operand);
#warning add debug output
                    if (operand == null) return null;
                    return Expression.MakeUnary(e_unary.NodeType, operand, e_unary.Type);
                }

                if (e_unary.NodeType == ExpressionType.Not) return e_unary.Operand;
            }

            if (E.Type == TypeOf.Bool)
                return Expression.MakeUnary(ExpressionType.Not, E, TypeOf.Bool);

#warning add debug output
            return null;
        }

        /// <summary>
        /// Returns function, that represents the predicate in the current state of context. 
        /// </summary>
        public static Expression PreEvaluate(Expression E) => internal_PreEvaluate(E) ?? E;

#warning !!! >> Constant does not turn into null, this probably means that new expression always created instead of returning the same ref
        private static Expression internal_PreEvaluate(Expression E)
        {
            switch (E.NodeType)
            {
                case ExpressionType.Quote:
                    {
                        var e_unary = E as UnaryExpression;
#warning implement later
                        if (e_unary.Method != null)
                            throw new NotImplementedException("Quote + Method");

                        var new_operand = internal_PreEvaluate(e_unary.Operand);
                        if (new_operand == null) return null;
                        return Expression.MakeUnary(ExpressionType.Quote, new_operand, E.Type);
                    };

                case ExpressionType.Lambda:
                    {
                        var e_lambda = E as LambdaExpression;
                        var new_body = internal_PreEvaluate(e_lambda.Body);
                        if (new_body == null) return null;
                        return Expression.Lambda(new_body, e_lambda.Parameters);
                    };

                case ExpressionType.Coalesce: // ??
                case ExpressionType.ArrayIndex: // []
                case ExpressionType.LeftShift: // <<
                case ExpressionType.RightShift: // >>
                case ExpressionType.Add: // +
                case ExpressionType.Subtract: // -
                case ExpressionType.Multiply: // *
                case ExpressionType.Divide: // /
                case ExpressionType.Modulo: // %
                case ExpressionType.LessThan: // <
                case ExpressionType.LessThanOrEqual: // <=
                case ExpressionType.GreaterThan: // >
                case ExpressionType.GreaterThanOrEqual: // >=
                case ExpressionType.AndAlso: // &&
                case ExpressionType.OrElse: // ||
                case ExpressionType.Equal: // ==
                case ExpressionType.NotEqual: // !=
                case ExpressionType.Or: // |
                case ExpressionType.And: // &
                case ExpressionType.ExclusiveOr: // ^
#warning next ones have not been tested yet
                case ExpressionType.Power:
                case ExpressionType.AddChecked:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.SubtractChecked:
                    {
                        var e_binary = E as BinaryExpression;
#warning implement later
                        if (e_binary.Conversion != null) throw new NotImplementedException("BinaryExpression.Conversion");
                        if (e_binary.Method != null) throw new NotImplementedException("BinaryExpression.Method");
                        if (e_binary.IsLifted) throw new NotImplementedException("BinaryExpression.IsLifted");
                        if (e_binary.IsLiftedToNull) throw new NotImplementedException("BinaryExpression.IsLiftedToNull");

                        var new_left = internal_PreEvaluate(e_binary.Left);
                        var new_right = internal_PreEvaluate(e_binary.Right);
                        if (new_left == null && new_right == null) return null;

                        if (new_left == null) new_left = e_binary.Left;
                        if (new_right == null) new_right = e_binary.Right;

                        var const_left = new_left as ConstantExpression;
                        var const_right = new_right as ConstantExpression;

                        if (E.NodeType == ExpressionType.AndAlso)
                        {
                            if (const_left != null)
                            {
                                if ((bool)const_left.Value == false) return Expression.Constant(false);
                                if ((bool)const_left.Value == true) return new_right;
                            }
                            if (const_right != null)
                            {
                                if ((bool)const_right.Value == false) return Expression.Constant(false);
                                if ((bool)const_right.Value == true) return new_left;
                            }
                        }

                        if (E.NodeType == ExpressionType.OrElse)
                        {
                            if (const_left != null)
                            {
                                if ((bool)const_left.Value == true) return Expression.Constant(true);
                                if ((bool)const_left.Value == false) return new_right;
                            }
                            if (const_right != null)
                            {
                                if ((bool)const_right.Value == true) return Expression.Constant(true);
                                if ((bool)const_right.Value == false) return new_left;
                            }
                        }

                        if (E.NodeType == ExpressionType.ExclusiveOr)
                        {
                            if (const_left != null) if ((bool)const_left.Value == false) return new_right;
                            if (const_right != null) if ((bool)const_right.Value == false) return new_left;
                        }

                        var newexpr = Expression.MakeBinary(E.NodeType, new_left, new_right);

                        if (const_left == null || const_right == null) return newexpr;

#warning can this be more efficient?
                        return Expression.Constant(Expression.Lambda(newexpr).Compile().DynamicInvoke(null), E.Type);
                    };

                case ExpressionType.MemberAccess:
                    {
                        var e_member = E as MemberExpression;
                        var member = e_member.Member;
                        switch(e_member.Expression.NodeType)
                        {
                            case ExpressionType.Parameter:
                                // cannot pre-evaluate lambda parameter value
                                return null; 

                            case ExpressionType.Constant:
                                {
                                    var field = member as FieldInfo;
                                    if (field != null) return Expression.Constant(field.GetValue((e_member.Expression as ConstantExpression).Value));

                                    throw new NotImplementedException($"Member access method is not implemented: {member.GetType().Name}");
                                }
                            default:
                                throw new NotImplementedException($"Member access target is not implemented: {e_member.Expression.NodeType}");
                        }
                    };

                case ExpressionType.Convert: // ()...
                case ExpressionType.TypeAs: // as
                case ExpressionType.ArrayLength: // .Length
                case ExpressionType.Negate: // -
                case ExpressionType.Not: // !
#warning next ones have not been tested yet
                case ExpressionType.UnaryPlus: // +...
                case ExpressionType.NegateChecked:
                case ExpressionType.ConvertChecked:
                case ExpressionType.OnesComplement: // ~
                    {
                        var e_unary = E as UnaryExpression;

#warning implement later
                        if (e_unary.Method != null) throw new NotImplementedException("BinaryExpression.Method");
                        if (e_unary.IsLifted) throw new NotImplementedException("BinaryExpression.IsLifted");
                        if (e_unary.IsLiftedToNull) throw new NotImplementedException("BinaryExpression.IsLiftedToNull");

                        var new_operand = internal_PreEvaluate(e_unary.Operand);
                        if (new_operand == null) return null;

                        var new_expression = Expression.MakeUnary(E.NodeType, new_operand, E.Type);

                        var const_operand = new_operand as ConstantExpression;
                        if (const_operand == null) return new_expression;

#warning can this be more efficient?
                        return Expression.Constant(Expression.Lambda(new_expression).Compile().DynamicInvoke(null), E.Type);
                    }

                case ExpressionType.Conditional: // ?:
                    {
                        var e_cond = E as ConditionalExpression;
                        bool unchanged = true;

                        var new_test = internal_PreEvaluate(e_cond.Test);
                        if (new_test == null) new_test = e_cond.Test;
                        else unchanged = false;

                        var const_test = new_test as ConstantExpression;
                        if (const_test != null)
                        {
                            bool test = (bool)const_test.Value;
                            var exptoreturn = test ? e_cond.IfTrue : e_cond.IfFalse;
                            return internal_PreEvaluate(exptoreturn) ?? exptoreturn;
                        }

                        var new_true = internal_PreEvaluate(e_cond.IfTrue);
                        if (new_true == null) new_true = e_cond.IfTrue;
                        else unchanged = false;

                        var new_false = internal_PreEvaluate(e_cond.IfFalse);
                        if (new_false == null) new_false = e_cond.IfFalse;
                        else unchanged = false;

                        if (unchanged) return null;

                        return Expression.Condition(new_test, new_true, new_false);
                    }

                case ExpressionType.NewArrayInit: // new [] {...}
                case ExpressionType.NewArrayBounds: // new [...]
                    {
#warning constant case is not processed
                        var e_init = E as NewArrayExpression;
                        var args = e_init.Expressions.Select(internal_PreEvaluate).ToArray();
                        if (args.All(a => a == null)) return null;
                        for (int i = 0; i < args.Length; i++) if (args[i] == null) args[i] = e_init.Expressions[i];

                        switch (E.NodeType)
                        {
                            case ExpressionType.NewArrayInit: return Expression.NewArrayInit(E.Type, args);
                            case ExpressionType.NewArrayBounds: return Expression.NewArrayBounds(E.Type, args);
                            default: throw new InvalidProgramException("Invalid switch statement in the code.");
                        }
                    }

                case ExpressionType.TypeIs: // is
                    {
                        var e_is = E as TypeBinaryExpression;
                        var new_expression = internal_PreEvaluate(e_is.Expression);
                        if (new_expression == null) return null;

                        var res = Expression.TypeIs(new_expression, e_is.TypeOperand);

                        var new_expression_constant = new_expression as ConstantExpression;
                        if (new_expression_constant == null) return res;

#warning this can be more efficient!!!
                        return Expression.Constant(Expression.Lambda(res).Compile().DynamicInvoke(null), E.Type);
                    }              
                    
                case ExpressionType.Call:
                    {
                        var e_call = E as MethodCallExpression;

                        bool unchanged = true;

                        var new_object = e_call.Object;
                        if (new_object != null)
                        {
                            new_object = internal_PreEvaluate(new_object);
                            if (new_object == null) new_object = e_call.Object;
                            else unchanged = false;
                        }

                        var new_args = e_call.Arguments.Select(internal_PreEvaluate).ToArray();
                        if (unchanged) if (new_args.Any(a => a != null)) unchanged = false;


                        MethodCallExpression result = null;

                        if (unchanged) result = e_call;
                        else
                        {
                            for (int i = 0; i < new_args.Length; i++) if (new_args[i] == null) new_args[i] = e_call.Arguments[i];
                            result = Expression.Call(new_object, e_call.Method, new_args);
                        }

#warning IMPLEMENT!!!!!
                        bool HasSideEffects = false;

                        if (!HasSideEffects) if (new_object == null || new_object is ConstantExpression) if (new_args.All(a=> a is ConstantExpression))
#warning can this be more efficient?
                                    return Expression.Constant(Expression.Lambda(result).Compile().DynamicInvoke(null), E.Type);

                        return result;
                    }

                case ExpressionType.Invoke:
                    {
                        var e_invoke = E as InvocationExpression;
                        bool unchanged = true;

                        var new_args = e_invoke.Arguments.Select(internal_PreEvaluate).ToArray();
                        var new_del = internal_PreEvaluate(e_invoke.Expression);

                        if (new_del == null) new_del = e_invoke.Expression;
                        else unchanged = false;

                        if (unchanged) if (new_args.Any(a => a != null)) unchanged = false;

                        InvocationExpression result = null;
                        if (unchanged) result = e_invoke;
                        else
                        {
                            for (int i = 0; i < new_args.Length; i++) if (new_args[i] == null) new_args[i] = e_invoke.Arguments[i];
                            result = Expression.Invoke(new_del, new_args);
                        }

#warning EXTRACT METHOD FROM DELEGATE AND IMPLEMENT!!!!!
                        bool HasSideEffects = false;
                        if (!HasSideEffects) if (new_args.All(a => a is ConstantExpression))
#warning can this be more efficient?
                                return Expression.Constant(Expression.Lambda(result).Compile().DynamicInvoke(null), E.Type);

                        return result;
                    }

                case ExpressionType.New:
                    {
                        var e_new = E as NewExpression;
                        if (e_new.Members != null)
#warning new {...}
                            throw new NotImplementedException("ExpressionType.New, Members");

                        var new_args = e_new.Arguments.Select(internal_PreEvaluate).ToArray();
                        if (new_args.All(a => a == null)) return null;

                        return Expression.New(e_new.Constructor, new_args);
                        // creating new object cannot be turned into returning the same constant even if arguments are constants!
                    }

                // this ones dont seem to be useful
#warning implement later
                case ExpressionType.Extension:
                case ExpressionType.Unbox:
                case ExpressionType.MemberInit: // new ...() {.. = ..}
                case ExpressionType.ListInit: // new ...() {..., ...}
#warning implement ExpressionType.ListInit first
                case ExpressionType.Index: 
                    {
                        throw new NotImplementedException($"{E.NodeType} is not implemented yet");
                        return null;
                    }

                case ExpressionType.Constant:
                    // Higher expressions won't be reduced if null were returned, therefore we return an unchanged constant.
                    return E;

#warning is that for sure?
                case ExpressionType.Dynamic:
                    throw new InvalidProgramException("An expression tree may not contain a dynamic operation");

#warning is that all for sure?
                case ExpressionType.RuntimeVariables:
                case ExpressionType.Loop:
                case ExpressionType.Label:
                case ExpressionType.Goto:
                case ExpressionType.Throw:
                case ExpressionType.Switch:
                case ExpressionType.Block:
                case ExpressionType.Try:
                    throw new InvalidProgramException("A lambda expression with a statement body cannot be converted to an expression tree");

#warning is that for sure?
                case ExpressionType.AddAssignChecked:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.SubtractAssignChecked:
                case ExpressionType.Assign:
                case ExpressionType.AddAssign:
                case ExpressionType.AndAssign:
                case ExpressionType.DivideAssign:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.ModuloAssign:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.SubtractAssign:
                case ExpressionType.OrAssign:
                case ExpressionType.PowerAssign:
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PostDecrementAssign:
                    throw new InvalidProgramException("An expression tree may not contain an assignment operator");

#warning unable to test next ones
                case ExpressionType.Parameter:
                case ExpressionType.Default:
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                case ExpressionType.Increment:
                case ExpressionType.Decrement:
                case ExpressionType.DebugInfo:
                case ExpressionType.TypeEqual:
                default:
                    throw new NotImplementedException($"Expression type is not implemented: {E.NodeType}");
            }
        } 
    }

}
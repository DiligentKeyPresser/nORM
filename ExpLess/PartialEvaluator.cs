using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpLess
{
    public static class PartialEvaluator
    {
        /// <summary>
        /// This function converts all the input expression and returns an array with the same order
        /// or `null` if all the elements remain the same
        /// </summary>
        private static Expression[] ConvertMany(ICollection<Expression> input)
        {
            bool anynew = false;
#warning Emit + recursion => using call stack to store elements can allow to avoid creating redundant arrays when `anynew` remains `false` 
            var result = new Expression[input.Count];
            for (int i = 0; i < result.Length; i++)
            {
                var oldarg = input.ElementAt(i);
                var newarg = PreEvaluate(oldarg);
                if (oldarg != newarg) anynew = true;
                result[i] = newarg;
            }
            return anynew ? result : null;
        }

        public static Expression PreEvaluate(Expression E)
        {
            switch (E.NodeType)
            {
                case ExpressionType.Quote:
                    {
                        var e_unary = E as UnaryExpression;
#warning implement later
                        if (e_unary.Method != null)
                            throw new NotImplementedException("Quote + Method");

                        var new_operand = PreEvaluate(e_unary.Operand);
                        if (new_operand == e_unary.Operand) return e_unary;
                        return Expression.MakeUnary(ExpressionType.Quote, new_operand, E.Type);
                    };

                case ExpressionType.Lambda:
                    {
                        var e_lambda = E as LambdaExpression;
                        var new_body = PreEvaluate(e_lambda.Body);
                        if (new_body == e_lambda.Body) return e_lambda;
                        return Expression.Lambda(new_body, e_lambda.Parameters);
                    };

                #region binary expressions

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

                        var new_left = PreEvaluate(e_binary.Left);
                        var new_right = PreEvaluate(e_binary.Right);

                        var const_left = new_left as ConstantExpression;
                        var const_right = new_right as ConstantExpression;

                        if (const_left != null || const_right != null)
                        {
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

#warning what if bitwise????
                            if (E.NodeType == ExpressionType.ExclusiveOr)
                            {
                                if (const_left != null) if ((bool)const_left.Value == false) return new_right;
                                if (const_right != null) if ((bool)const_right.Value == false) return new_left;
                            }
                        }

                        var newexpr = new_left == e_binary.Left && new_right == e_binary.Right ? e_binary : Expression.MakeBinary(E.NodeType, new_left, new_right);
                        if (const_left == null || const_right == null) return newexpr;

#warning can this be more efficient?
                        return Expression.Constant(Expression.Lambda(newexpr).Compile().DynamicInvoke(null), E.Type);
                    };

                #endregion

                case ExpressionType.MemberAccess:
                    {
                        var e_member = E as MemberExpression;
                        var member = e_member.Member;
                        switch (e_member.Expression.NodeType)
                        {
                            case ExpressionType.Parameter:
                                // cannot pre-evaluate lambda parameter value
                                return e_member;

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

                        var new_operand = PreEvaluate(e_unary.Operand);

                        var new_expression = new_operand == e_unary.Operand ? e_unary : Expression.MakeUnary(E.NodeType, new_operand, E.Type);

                        var const_operand = new_operand as ConstantExpression;
                        if (const_operand == null) return new_expression;

#warning can this be more efficient?
                        return Expression.Constant(Expression.Lambda(new_expression).Compile().DynamicInvoke(null), E.Type);
                    }

                case ExpressionType.Conditional: // ?:
                    {
                        var e_cond = E as ConditionalExpression;
                        bool unchanged = true;

                        var new_test = PreEvaluate(e_cond.Test);
                        if (new_test != e_cond.Test) unchanged = false;

                        var const_test = new_test as ConstantExpression;
                        if (const_test != null)
                        {
                            bool test = (bool)const_test.Value;
                            var exptoreturn = test ? e_cond.IfTrue : e_cond.IfFalse;
                            return PreEvaluate(exptoreturn);
                        }

                        var new_true = PreEvaluate(e_cond.IfTrue);
                        if (new_true != e_cond.IfTrue) unchanged = false;

                        var new_false = PreEvaluate(e_cond.IfFalse);
                        if (new_false != e_cond.IfFalse)  unchanged = false;

                        return unchanged ? e_cond : Expression.Condition(new_test, new_true, new_false);
                    }

                case ExpressionType.NewArrayInit: // new [] {...}
                case ExpressionType.NewArrayBounds: // new [...]
                    {
#warning constant case is not processed
                        var e_init = E as NewArrayExpression;
                        var args = ConvertMany(e_init.Expressions);
                        if (args == null) return e_init;

                        // these operators dont seem to be eligible for constant conversion
                        switch (E.NodeType)
                        {
                            case ExpressionType.NewArrayInit: return Expression.NewArrayInit(E.Type, args);
                            case ExpressionType.NewArrayBounds: return Expression.NewArrayBounds(E.Type, args);
                            default: throw new NotImplementedException("Invalid switch statement in the code.");
                        }
                    }

                case ExpressionType.TypeIs: // is
                    {
                        var e_is = E as TypeBinaryExpression;
                        var new_expression = PreEvaluate(e_is.Expression);
                        if (new_expression == e_is.Expression) return e_is;

                        var res = Expression.TypeIs(new_expression, e_is.TypeOperand);

                        var new_expression_constant = new_expression as ConstantExpression;
                        if (new_expression_constant == null) return res;

#warning this can be more efficient!!!
                        return Expression.Constant(Expression.Lambda(res).Compile().DynamicInvoke(null), E.Type);
                    }

                case ExpressionType.Call:
                    {
                        var e_call = E as MethodCallExpression;

                        ICollection<Expression> new_args = ConvertMany(e_call.Arguments);
                        bool unchanged = new_args == null;
                        if (unchanged)
                            new_args = e_call.Arguments;

                        var new_object = e_call.Object == null ? null : PreEvaluate(e_call.Object);
                        if (new_object != e_call.Object) unchanged = false;

                        MethodCallExpression result = null;

                        if (unchanged) result = e_call;
                        else result = Expression.Call(new_object, e_call.Method, new_args);

#warning IMPLEMENT!!!!!
                        bool HasSideEffects = false;

                        if (!HasSideEffects)
                            if (new_object == null || new_object is ConstantExpression)
                                if (new_args.All(a => a is ConstantExpression))
#warning can this be more efficient?
                                    return Expression.Constant(Expression.Lambda(result).Compile().DynamicInvoke(null), E.Type);

                        return result;
                    }

                case ExpressionType.Invoke:
                    {
                        var e_invoke = E as InvocationExpression;

                        ICollection<Expression> new_args = ConvertMany(e_invoke.Arguments);
                        var new_del = PreEvaluate(e_invoke.Expression);

                        bool unchanged = new_args == null;
                        if (unchanged)
                            new_args = e_invoke.Arguments;

                        if (new_del != e_invoke.Expression) unchanged = false;

                        InvocationExpression result = null;
                        if (unchanged) result = e_invoke;
                        else result = Expression.Invoke(new_del, new_args);

#warning EXTRACT METHOD FROM DELEGATE AND IMPLEMENT!!!!!
                        bool HasSideEffects = false;
                        if (!HasSideEffects)
                            if (new_args.All(a => a is ConstantExpression))
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
                        
                        var new_args = ConvertMany(e_new.Arguments);
                            
                        // creating new object cannot be turned into returning the same constant even if arguments are constants!
                        return new_args == null ? e_new : Expression.New(e_new.Constructor, new_args);
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
                    }

                case ExpressionType.Constant:
                    return E;

#warning is that for sure?
                case ExpressionType.Dynamic:
                    throw new NotImplementedException("An expression tree may not contain a dynamic operation");

#warning is that all for sure?
                case ExpressionType.RuntimeVariables:
                case ExpressionType.Loop:
                case ExpressionType.Label:
                case ExpressionType.Goto:
                case ExpressionType.Throw:
                case ExpressionType.Switch:
                case ExpressionType.Block:
                case ExpressionType.Try:
                    throw new NotImplementedException("A lambda expression with a statement body cannot be converted to an expression tree");

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
                    throw new NotImplementedException("An expression tree may not contain an assignment operator");

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


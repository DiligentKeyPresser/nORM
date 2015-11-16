using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace MakeSQL
{
    public abstract class SQLContext
    {
        internal SQLContext() { }

        internal virtual string GetFunctionName(SqlFunction Function)
        {
            switch (Function)
            {
                case SqlFunction.Count: return "COUNT";
                default: throw new NotSupportedException($"The given function (`{Function.ToString()}`) is not supporthed in the surrent context.");
            }
        }

        internal virtual string GetTypeName(Type type)
        {
            switch (type.Name)
            {
                case nameof(Boolean) : return "BOOLEAN";
                default: throw new NotSupportedException($"The given type (`{type.Name}`) is not supporthed in the surrent context.");
            }
        }

        internal virtual IEnumerator<string> EscapeLiteral(object Value)
        {
            if (Value.GetType() == typeof(int))
            {
                yield return Value.ToString();
                yield break; 
            }
            throw new NotSupportedException($"The type of the given literal (`{Value.GetType().Name}`) is not supporthed in the surrent context.");
        }

        internal virtual string LeftEscapingSymbol => "\"";
        internal virtual string RightEscapingSymbol => "\"";

        protected static string[] MakeBinary(string[] Left, string op, string[] Right)
        {
            var result = new string[Left.Length + 1 + Right.Length];
            Array.Copy(Left, result, Left.Length);
            result[Left.Length] = op;
            Array.Copy(Right, 0, result, Left.Length + 1, Right.Length);
            return result;
        }

#warning !!!
        public string[] BuildPredicate(Expression E, Expression Row, Func<MemberInfo, LocalIdentifier> FieldResolver)
        {
            var e_constant = E as ConstantExpression;
            if (e_constant != null)
            {
                switch (e_constant.Type.Name)
                {
                    case nameof(String):
                        return new string[] { (string)e_constant.Value };

                    case nameof(Boolean):
                        return new string[] { (bool)e_constant.Value ? "1 = 1" : "1 = 0" };

                    case nameof(Int16):
                    case nameof(Int32):
                        return new string[] { e_constant.Value.ToString() };

                    default:
#warning add debug output
                        return null;
                }
            }

            var e_member = E as MemberExpression;
            if (e_member != null)
            {
                var RowPropertyAccess = e_member.Expression == Row;
                if (RowPropertyAccess) return new string[] { FieldResolver(e_member.Member).Build(this) };

#warning add debug output
                return null;
            }

            var e_binary = E as BinaryExpression;
            if (e_binary != null)
            {
#warning do lifted calls need any special processing?
#warning does 'method' need any special processing?

                var Left = BuildPredicate(e_binary.Left, Row, FieldResolver);
                if (Left == null)
#warning add debug output
                    return null;
                var Right = BuildPredicate(e_binary.Right, Row, FieldResolver);
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
                        throw new NotSupportedException($"some unknown binary operator has been passed: {e_binary.NodeType.ToString()}");
                }
            }

            var e_lambda = E as LambdaExpression;
            if (e_lambda != null) return BuildPredicate(e_lambda.Body, e_lambda.Parameters[0], FieldResolver);

            var e_unary = E as UnaryExpression;
            if (e_unary != null)
            {
                switch (e_unary.NodeType)
                {
                    case ExpressionType.Not:
                        var positive = BuildPredicate(e_unary.Operand, Row, FieldResolver);
                        var negative = new string[positive.Length + 2];
                        negative[0] = "NOT(";
                        Array.Copy(positive, 0, negative, 1, positive.Length);
                        negative[negative.Length - 1] = ") ";
                        return negative;
                    case ExpressionType.Quote: return BuildPredicate(e_unary.Operand, null, FieldResolver);
                    default:
#warning add debug output
                        return null;
                }


            }
#warning add debug output
            return null;
        }

    }
}
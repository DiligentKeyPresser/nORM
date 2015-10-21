using System;
using System.Linq.Expressions;

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
                var Right = ToSQL<RowContract>(e_binary.Right, Row);

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
                    case ExpressionType.Not:
                    case ExpressionType.NotEqual:
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                    case ExpressionType.Parameter:
                    case ExpressionType.Power:
                    case ExpressionType.Quote:
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
#if DEBUG
#warning ????
                if (e_unary.Operand == null) throw new NotImplementedException("where clause: unary expressions without operand are not supported");
#endif
                return ToSQL<RowContract>(e_unary.Operand, null);
            }



#warning add debug output
            return null;
        }



    }

}
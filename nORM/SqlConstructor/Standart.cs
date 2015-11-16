using System;
using System.Linq.Expressions;
using System.Text;

namespace nORM
{
    namespace SQL
    {
        [Obsolete("old", true)]
        internal abstract class StandartSQLQueryFactory : IQueryFactory
        {
            public virtual string EscapeIdentifier(string schema, string name)
            {
                // The SQL-99 standard specifies that double quote (") is used to delimit identifiers.
                var builder = new StringBuilder();

                builder.Append("\"");
                if (!string.IsNullOrEmpty(schema))
                {
                    builder.Append(schema);
                    builder.Append("\".\"");
                }
                builder.Append(name);
                builder.Append("\"");

                return builder.ToString();
            }

            public string[] CreatePredicate<RowContract>(Expression E)
            {
#warning обрабатываются только предикаты с одним аргументом
                return ToSQL<RowContract>(E, null);
            }

            /// <summary>
            /// Builds array from two arays and one string between them
            /// </summary>
            protected static string[] MakeBinary(string[] Left, string op, string[] Right)
            {
                var result = new string[Left.Length + 1 + Right.Length];
                Array.Copy(Left, result, Left.Length);
                result[Left.Length] = op;
                Array.Copy(Right, 0, result, Left.Length + 1, Right.Length);
                return result;
            }

            private string[] ToSQL<RowContract>(Expression E, Expression Row)
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
                        return new string[] { EscapeIdentifier(null, /*(Attribute.GetCustomAttribute(e_member.Member, TypeOf.FieldAttribute) as FieldAttribute).ColumnName)*/"") };
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

            public abstract SelectQuery Select(string source, string[] fields, string SourceAlias);
        }

        [Obsolete("old", true)]
        internal abstract class StandartSQLSelectQuery : SelectQuery
        {
            protected readonly string source;
            protected readonly string source_alias;
            protected string[] fields;
            protected string[] where;

            protected static readonly string[] StandartCountClause = new string[] { "COUNT(*)" };

            internal StandartSQLSelectQuery(string source, string[] fields, string SourceAlias)
            {
                this.source = source;
                this.fields = fields;
                where = new string[0];
                source_alias = SourceAlias;
            }

            protected override string Build()
            {
                var builder = new StringBuilder();

                builder.Append("SELECT ");

                for (int i = 0; i < fields.Length; i++)
                {
                    builder.Append(fields[i]);
                    if (i < fields.Length - 1) builder.Append(", ");
                }

                builder.Append(" FROM ");

                if (source_alias != null) builder.Append("(");
                builder.Append(source);
                if (source_alias != null)
                {
                    builder.Append(") AS ");
                    builder.Append(source_alias);
                }

                if (where.Length > 0)
                {
                    builder.Append(" WHERE ");
                    bool brackets = where.Length > 1;

                    for (int i = 0; i < where.Length; i++)
                    {
                        if (brackets) builder.Append("(");
                        builder.Append(where[i]);
                        if (brackets) builder.Append(")");

                        if (i < where.Length - 1) builder.Append(" AND ");
                    }
                }

                return builder.ToString();
            }

            public override SelectQuery Where(string clause)
            {
                var clone = Clone() as StandartSQLSelectQuery;

                var new_where = new string[where.Length + 1];
                Array.Copy(where, new_where, where.Length);
                new_where[where.Length] = clause;

                clone.where = new_where;
                return clone;
            }

            public override SelectQuery MakeCount()
            {
                var clone = Clone() as StandartSQLSelectQuery;
                clone.fields = StandartCountClause;
                return clone;
            }

            public override SelectQuery MakeLongCount()
            {
#warning we could probably use COUNT(*) instead, but COUNT() is tooo slow on PostgreSQL
                throw new NotImplementedException("COUNT_BIG is not available in this implementation");
            }
        }
    }
}
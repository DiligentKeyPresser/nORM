using System.Linq.Expressions;

namespace nORM
{
    internal static class PredicateTranslator
    {
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
    }
}
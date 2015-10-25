using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace nORM
{
    internal abstract class RowProvider : IQueryProvider
    {
        #region Implemented method tokens

#warning how long does the initialization take?

        // We can search for all the implemented methods then store the metadata tokens into static fields in order to reduce reflection at runtime.

        /// <summary>
        /// This method extracts generic expressions from parameter types and selects types of expressions.
        /// This method is used to distinguish overloads when other parameters are the same. 
        /// </summary>
        private static IEnumerable<Type> ExtractExpressionArguments(Type[] Arguments) =>
            from parameter_type in Arguments
            where parameter_type.GetGenericTypeDefinition() == TypeOf.Expression_generic
            let expression_type = parameter_type.GetGenericArguments().Single()
            where expression_type.IsGenericType
            select expression_type.GetGenericTypeDefinition();

        /// <summary>
        /// Search for Queryable extension.
        /// </summary>
        /// <param name="Name"> Name of extension method. </param>
        /// <param name="Arguments"> Parameter types. </param>
        private static MethodInfo FindExtension(string Name, params Type[] Arguments)
        {
            // most overloads can be distinguished by generic type definions of its parameters
            var most_generic_definions = Arguments.Select(type => type.GetGenericTypeDefinition()).ToArray();

            // some overloads can take expressions with different delegate type.
            // we have to look into expression types.
            var expression_definions = ExtractExpressionArguments(Arguments).ToArray();

            return (from method in TypeOf.Queryable.GetMethods(BindingFlags.Static | BindingFlags.Public)
                    where method.Name == Name
                    let method_args_actual = method.GetParameters().Select(p => p.ParameterType).ToArray()
                    let method_args_generic = method_args_actual.Select(p => p.GetGenericTypeDefinition())
                    where method_args_generic.SequenceEqual(most_generic_definions)
                    let local_expression_definions = ExtractExpressionArguments(method_args_actual)
                    where local_expression_definions.SequenceEqual(expression_definions)
                    select method).Single();
        }

        protected static readonly int SimpleCount = FindExtension("Count", TypeOf.IQueryable_generic).MetadataToken;
        protected static readonly int PredicatedCount = FindExtension("Count", TypeOf.IQueryable_generic, typeof(Expression<Func<object, bool>>)).MetadataToken;
        protected static readonly int SimpleCountLong = FindExtension("LongCount", TypeOf.IQueryable_generic).MetadataToken;
        protected static readonly int PredicatedCountLong = FindExtension("LongCount", TypeOf.IQueryable_generic, typeof(Expression<Func<object, bool>>)).MetadataToken;
        protected static readonly int SimpleWhere = FindExtension("Where", TypeOf.IQueryable_generic, typeof(Expression<Func<object, bool>>)).MetadataToken;
#warning protected static readonly int IndexedWhereWhere = FindExtension("Where", TypeOf.IQueryable_generic, typeof(Expression<Func<object, int, bool>>)).MetadataToken;
#warning protected static readonly int SimpleSelect = FindExtension("Select", TypeOf.IQueryable_generic, typeof(Expression<Func<object, object>>)).MetadataToken;
#warning protected static readonly int SelectIndexed = FindExtension("Select", TypeOf.IQueryable_generic, typeof(Expression<Func<object, int, object>>)).MetadataToken;

        #endregion

        public IQueryable CreateQuery(Expression expression)
        {
            //Several of the standard query operator methods defined in Queryable, such as OfType<TResult> and Cast<TResult>, call this method.
#warning must be implemented   
            throw new NotImplementedException();
        }

        public abstract IQueryable<TElement> CreateQuery<TElement>(Expression expression);


        // The Execute method executes queries that return a single value (instead of an enumerable sequence of values). 
        // Expression trees that represent queries that return enumerable results are executed when the IQueryable<T> object that contains the expression tree is enumerated.

        TResult IQueryProvider.Execute<TResult>(Expression expression) => (TResult)execute_scalar(expression);
        object IQueryProvider.Execute(Expression expression) => execute_scalar(expression);
        protected abstract object execute_scalar(Expression expression);
    }

    internal class RowProvider<SourceRowContract> : RowProvider
    {
        #region Singleton
        protected RowProvider() { }
        public static RowProvider<SourceRowContract> Singleton { get; } = new RowProvider<SourceRowContract>();
        #endregion

        public override IQueryable<TResultElement> CreateQuery<TResultElement>(Expression expression)
        {
            var mc_expr = expression as MethodCallExpression;

            // проверка структуры дерева выражения, в релизе будем ее опускать
#if DEBUG
            if (mc_expr == null) throw new InvalidProgramException("range query must be a method call.");
            if (mc_expr.Method.DeclaringType != TypeOf.Queryable) throw new InvalidProgramException("query method must be a Queryable member");
#endif
            var const_arg_0 = mc_expr.Arguments[0] as ConstantExpression;
#if DEBUG
            if (const_arg_0 == null) throw new InvalidProgramException("range query method must be applyed to source directly");
            if (!(const_arg_0.Value is RowSource<SourceRowContract>)) throw new InvalidProgramException($"range query method must be applyed to RowSource<{typeof(SourceRowContract).Name}>");
#endif 
            var TargetObject = const_arg_0.Value as RowSource<SourceRowContract>;
            var MethodToken = mc_expr.Method.MetadataToken;

            if (MethodToken == SimpleWhere)
            {
#if DEBUG
                if (typeof(SourceRowContract) != typeof(TResultElement)) throw new InvalidProgramException("Where query method must be applyed to RowSource of the same type");
#endif 
                var new_query = TargetObject.MakeWhere(mc_expr.Arguments[1]);
#warning add debug output
                if (new_query == null) goto failed_to_translate;
                else return new_query as RowSource<TResultElement>;
            }

            /*
            if (mc_expr.Method.MetadataToken == SimpleSelect.MetadataToken)
            {
                var select_target = TargetObject as RowSource<SourceRowContract>;
                var new_query = select_target.MakeProjection<TResultElement>(mc_expr.Arguments[1]);
    #warning is this even possible?
                if (new_query == null) goto failed_to_translate;
                else return new_query;
            }
            */

            failed_to_translate:
            // попадаем сюда если пришедший метод не транслируется в SQL
            // и делегируем выполение дальнейшей работы поставщику LinqToObjects

#warning SELECT is still unefficient
            var Materialized = TargetObject.Materialize();

            var NewArguments = new Expression[mc_expr.Arguments.Count];
            NewArguments[0] = Expression.Constant(Materialized);
            for (int i = 1; i < mc_expr.Arguments.Count; i++)
                NewArguments[i] = mc_expr.Arguments[i];

            return Materialized.Provider.CreateQuery<TResultElement>(mc_expr.Update(null, NewArguments));
        }

        protected override object execute_scalar(Expression expression)
        {
            var mc_expr = expression as MethodCallExpression;

            // проверка структуры дерева выражения, в релизе будем ее опускать
#if DEBUG
            if (mc_expr == null) throw new InvalidProgramException("scalar query must be a method call.");
            if (mc_expr.Method.DeclaringType != TypeOf.Queryable) throw new InvalidProgramException("query method must be a Queryable memder.");
#endif 
            var const_arg_0 = mc_expr.Arguments[0] as ConstantExpression;
#if DEBUG
            if (const_arg_0 == null) throw new InvalidProgramException("scalar query method must be applyed to source directly");
            if (!(const_arg_0.Value is RowSource<SourceRowContract>)) throw new InvalidProgramException($"scalar query method must be applyed to RowSource<{typeof(SourceRowContract).Name}>");
#endif
            var TargetObject = const_arg_0.Value as RowSource<SourceRowContract>;

            // рассматриваем различные скалярные штуки
            var MethodToken = mc_expr.Method.GetGenericMethodDefinition().MetadataToken;

            bool
                isPredicatedScalar = false,
                isCount = false,
                isLongCount = false;

            if (MethodToken == SimpleCount)
            {
                isCount = true;
                goto try_to_translate;
            }

            if (MethodToken == PredicatedCount)
            {
                isCount = true;
                isPredicatedScalar = true;
                goto try_to_translate;
            }

            if (MethodToken == SimpleCountLong)
            {
                isCount = true;
                isLongCount = true;
                goto try_to_translate;
            }

            if (MethodToken == PredicatedCountLong)
            {
                isCount = true;
                isLongCount = true;
                isPredicatedScalar = true;
                goto try_to_translate;
            }

        try_to_translate:

            var PredicatedTarget = isPredicatedScalar ? TargetObject.MakeWhere(mc_expr.Arguments[1]) : TargetObject;
            if (PredicatedTarget == null) goto failed_to_translate;

            if (isCount)
            {
                var select_list_length = PredicatedTarget.SelectListLength;
                var select_list_start = PredicatedTarget.SelectListStart;
                var select_list_end = select_list_start + select_list_length;

                var old_sql_query = PredicatedTarget.GetSQLArray();
                var new_sql_query = new string[old_sql_query.Length + 3 - select_list_length];

                Array.Copy(old_sql_query, new_sql_query, select_list_start);
                new_sql_query[select_list_start] = isLongCount ? "COUNT_BIG(*) " : "COUNT(*) ";
                Array.Copy(old_sql_query, select_list_end, new_sql_query, select_list_start + 1, old_sql_query.Length - select_list_end);

                var SqlQuery = string.Concat(new_sql_query);
                return PredicatedTarget.Context.ExecuteScalar(SqlQuery);
            }

            goto failed_to_translate;


            failed_to_translate:
            // попадаем сюда если пришедший метод не транслируется в SQL
            // и делегируем выполение дальнейшей работы поставщику LinqToObjects

            var Materialized = TargetObject.Materialize();

            var NewArguments = new Expression[mc_expr.Arguments.Count];
            NewArguments[0] = Expression.Constant(Materialized);
            for (int i = 1; i < mc_expr.Arguments.Count; i++)
                NewArguments[i] = mc_expr.Arguments[i];

#warning why Execute works while Execute<SourceRowContract> throws an exception?
            return Materialized.Provider.Execute(mc_expr.Update(null, NewArguments));
        }
    }
}
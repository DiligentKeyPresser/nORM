using nORM.SQL;
using System;
using System.Linq;
using System.Linq.Expressions;

using static ExpLess.PartialEvaluator;

#warning сунуть как можно больше проверок в дебаг
#warning материализация в список - вероятно проблема. фикс должен коснуться как материализации, так и метода выполнения в контексте бд

namespace nORM
{
#warning непонятно, нужен ли такой тип
    internal abstract class DatabaseRow { }

#warning do we need this class?
    public abstract class RowSource
    {
        internal readonly SelectQuery theQuery;

        internal RowSource(DatabaseContext ConnectionContext, SelectQuery Query) 
        {
            Context = ConnectionContext;
            theQuery = Query;
        }

        internal DatabaseContext Context { get; }
    }

    /// <summary>
    /// Класс для представления любого объекта, из которого можно запросом получать строки - 
    /// таблицы, представления, результаты выполнения функций и других запросов
    /// </summary>
    /// <typeparam name="RowContract"> Тип строк, которые можно получить из данного объекта </typeparam>
    public partial class RowSource<RowContract> : RowSource, IQueryable<RowContract> 
    {
        protected static readonly Type contract_type = typeof(RowContract);
        /// <summary>
        /// Тип строк, которые можно получить из данного объекта
        /// </summary>
        public Type ElementType { get { return contract_type; } }
        public Expression Expression { get; }
        public IQueryProvider Provider { get { return RowProvider<RowContract>.Singleton; } }
        internal RowSource(DatabaseContext ConnectionContext, SelectQuery Query) : base(ConnectionContext, Query)
        {
            Expression = Expression.Constant(this);
        }

        internal RowSource<RowContract> MakeWhere(Expression Condition)
        {
            var sql_predicate = Context.QueryFactory.CreatePredicate<RowContract>(PreEvaluate(Condition));
#warning add Debug output
            if (sql_predicate == null) return null;

            var NewQuery = theQuery.Where(string.Concat(sql_predicate));
            return new RowSource<RowContract>(Context, NewQuery);
        }
    }

    public sealed class Table<RowContract> : RowSource<RowContract> 
    {
        private static readonly FieldAttribute[] FieldAttributes;

        private static string[] BuildSelectionList(DatabaseContext ConnectionContext) => FieldAttributes.Select(a => ConnectionContext.QueryFactory.EscapeIdentifier(null, a.ColumnName)).ToArray();

        static Table()
        {
            FieldAttributes = typeof(RowContract).GetProperties().Where(p => Attribute.IsDefined(p, TypeOf.FieldAttribute))
                .Select(p => Attribute.GetCustomAttribute(p, TypeOf.FieldAttribute) as FieldAttribute).ToArray();
        }

        /// <summary>
        /// Имя таблицы в базе данных
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Конструктор для вызова из динамического класса базы данных.
        /// Вручную не вызывается нигде.
        /// </summary>
        internal Table(DatabaseContext ConnectionContext, string TableName)
            : base(ConnectionContext, ConnectionContext.QueryFactory.Select(TableName, BuildSelectionList(ConnectionContext), null))
        {
            Name = TableName;            
        }
    }
    /*
    public class ProjectionQuery<TResultElement, TSourceRowContract>: Query<TResultElement>
    {

    }
    */
}

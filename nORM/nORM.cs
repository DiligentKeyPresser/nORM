using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using static ExpLess.PartialEvaluator;

#warning сунуть как можно больше проверок в дебаг
#warning материализация в список - вероятно проблема. фикс должен коснуться как материализации, так и метода выполнения в контексте бд

namespace nORM
{
#warning непонятно, нужен ли такой тип
    internal abstract class DatabaseRow { }
    
    /// <summary>
    /// Служебный класс, способный представлять любой SELECT к базе данных, возвращающий таблицу
    /// </summary>
    public abstract class RowSource
    {
        // Интерфейс не подходит изза ограничения на область видимости его членов

#warning А теперь кажется что проще хранить запрос непосредственно в виде строки
        private string sql_query = null;
        internal string GetSQL() => sql_query ?? (sql_query = string.Concat(GetSQLArray()));

        protected string[] sql_array;
        internal string[] GetSQLArray() => sql_array;

        internal RowSource(DatabaseContext ConnectionContext) 
        {
            Context = ConnectionContext;
        }

        // Обращение к данному свойству производится довольно часто, стоит сохранить ссылку, а не ходить в провайдер
        internal DatabaseContext Context { get; }

        abstract internal protected int SelectListStart { get; }
        abstract internal protected int SelectListLength { get; }
    }

    /// <summary>
    /// Класс для представления любого объекта, из которого можно запросом получать строки - 
    /// таблицы, представления, результаты выполнения функций и других запросов
    /// </summary>
    /// <typeparam name="RowContract"> Тип строк, которые можно получить из данного объекта </typeparam>
    public abstract partial class RowSource<RowContract> : RowSource, IQueryable<RowContract> 
    {
        protected static readonly Type contract_type = typeof(RowContract);
        /// <summary>
        /// Тип строк, которые можно получить из данного объекта
        /// </summary>
        public Type ElementType { get { return contract_type; } }
        public Expression Expression { get; }
        public IQueryProvider Provider { get { return RowProvider<RowContract>.Singleton; } }
        internal RowSource(DatabaseContext ConnectionContext) : base(ConnectionContext)
        {
            Expression = Expression.Constant(this);
        }
        internal abstract Query<RowContract> MakeWhere(Expression Condition);
        internal protected int NextWhereClausePosition { get; protected set; }
        internal protected bool HasWhereClause { get; protected set; }
    }

#warning IOrderedQueryable<RowContract> ???
#warning public???
#warning а не убрать ли типизацию?
    public class Query<RowContract> : RowSource<RowContract> 
    {
#warning наплодить конструкторов чтобы не слать через стек лишнее
        internal Query(RowSource<RowContract> Source, string[] sql, int SelectListStart, int SelectListLength, int NextWhereClausePosition,
            bool HasWhereClause = false
            ) : base(Source.Context)
        {
            this.Source = Source;
            this.sql_array = sql;
            this.HasWhereClause = HasWhereClause;
            this.NextWhereClausePosition = NextWhereClausePosition;
            this.SelectListStart = SelectListStart;
            this.SelectListLength = SelectListStart;
        }

        private RowSource<RowContract> Source;

        protected internal override int SelectListStart { get; }

        protected internal override int SelectListLength { get; }

        internal override Query<RowContract> MakeWhere(Expression Condition)
        {
            var sql_predicate = PredicateTranslator.TranslatePredicate<RowContract>(PreEvaluate(Condition));
#warning add Debug output
            if (sql_predicate == null) return null;

            var WhereClauseArrayLength = sql_predicate.Length;
            var new_sql_array = new string[sql_array.Length + WhereClauseArrayLength + 1];

            Array.Copy(sql_array, new_sql_array, NextWhereClausePosition);
            Array.Copy(sql_array, NextWhereClausePosition, new_sql_array, NextWhereClausePosition + WhereClauseArrayLength + 1, sql_array.Length - NextWhereClausePosition);
            Array.Copy(sql_predicate, 0, new_sql_array, NextWhereClausePosition + 1, WhereClauseArrayLength);
            new_sql_array[NextWhereClausePosition] = HasWhereClause ? " AND " : "WHERE ";

            return new Query<RowContract>(this, new_sql_array, SelectListStart, SelectListLength, new_sql_array.Length,
                HasWhereClause: true);
        }
    }

    public sealed class Table<RowContract> : RowSource<RowContract> 
    {
#warning может массивчик?
        private static readonly string selection_list;

        static Table()
        {
#warning порядок полей гарантирован???
            selection_list = string.Join(", ", typeof(RowContract).GetProperties().Where(p => Attribute.IsDefined(p, TypeOf.FieldAttribute))
                .Select(p => (Attribute.GetCustomAttribute(p, TypeOf.FieldAttribute) as FieldAttribute).ColumnName)) + " ";
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
            :base(ConnectionContext)
        {
            Name = TableName;
            sql_array = new string[] { "SELECT ", selection_list, "FROM ", Name, " " };
        } 

        internal override Query<RowContract> MakeWhere(Expression Condition)
        {
            var sql_predicate = PredicateTranslator.TranslatePredicate<RowContract>(PreEvaluate(Condition));
#warning add Debug output
            if (sql_predicate == null) return null;

            var WhereClauseArrayLength = sql_predicate.Length;
            var new_sql_array = new string[sql_array.Length + WhereClauseArrayLength + 1];

            Array.Copy(sql_array, new_sql_array, sql_array.Length);
            new_sql_array[sql_array.Length] = "WHERE ";
            Array.Copy(sql_predicate, 0, new_sql_array, sql_array.Length + 1, WhereClauseArrayLength);

            return new Query<RowContract>(this, new_sql_array, SelectListStart, SelectListLength, new_sql_array.Length,
                HasWhereClause: true);
        }

        protected internal override int SelectListStart => 1;

        protected internal override int SelectListLength => 1;
    }
    /*
    public class ProjectionQuery<TResultElement, TSourceRowContract>: Query<TResultElement>
    {

    }
    */
}

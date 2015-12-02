using MakeSQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static ExpLess.PartialEvaluator;
using System.Collections;

#warning сунуть как можно больше проверок в дебаг
#warning материализация в список - вероятно проблема. фикс должен коснуться как материализации, так и метода выполнения в контексте бд

namespace nORM
{
#warning непонятно, нужен ли такой тип
    internal abstract class DatabaseRow { }

#warning do we need this class?
    internal abstract class RowSource
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
    internal partial class RowSource<RowContract> : RowSource, IQueryable<RowContract> 
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
            var sql_predicate = Context.QueryContext.BuildPredicate(PreEvaluate(Condition), null, member=>
            {
#if DEBUG
                if (!Attribute.IsDefined(member, TypeOf.FieldAttribute)) throw new InvalidContractException(typeof(RowContract), "Field name is not defined.");
#endif
#warning не самый быстрый способ. не закешировать ли?
                return (Attribute.GetCustomAttribute(member, TypeOf.FieldAttribute) as FieldAttribute).ColumnName;
            });
#warning add Debug output
            if (sql_predicate == null) return null;

            var NewQuery = theQuery.Where(string.Concat(sql_predicate));
            return new RowSource<RowContract>(Context, NewQuery);
        }
    }

    internal class Table<RowContract> : RowSource<RowContract>, ITable<RowContract> 
    {
        /// <summary> Collection of columns of the table </summary>
        public IReadOnlyList<DataColumn> Columns => RowContractInfo<RowContract>.Columns;

        /// <summary> Gets a name of the table, based on contract declaration. </summary>
        internal QualifiedIdentifier Name { get; }

        /*
                public void Insert<SubRowContract>(SubRowContract OneValue) => Insert((IEnumerable<SubRowContract>)(new SubRowContract[] { OneValue }));

                [Obsolete("will be removed")]
                public TRes InsertRet<SubRowContract, TRes>(SubRowContract OneValue, DataColumn ReturningColumn)
                {
                    if (!typeof(TRes).IsAssignableFrom(ReturningColumn.ColumnType))
                        throw new InvalidContractException($"Column '{ReturningColumn.ContractName}' in a contract '{nameof(RowContract)}' cannot be cast to a type '{nameof(TRes)}'");

        #warning cache this
                    var SubRowColumns = RowContractInfo<SubRowContract>.Columns.Select(c => c.FieldName).ToArray();
                    var Query = new InsertQuery(Name, SubRowColumns, new Values(new object[][] { RowContractDecomposer<SubRowContract>.Decompose(OneValue) }), ReturningColumn.FieldName);
                    var SQL = Query.Query.Build(Context.QueryContext);
                    var res = Context.ExecuteScalar(SQL);
                    if (res == DBNull.Value)
                    {
                        if (Nullable.GetUnderlyingType(ReturningColumn.ColumnType) == null)
                            throw new InvalidContractException($"InsertReturning: 'NULL' value in a non-nullable field '{ReturningColumn.FieldName}'.");
                        return default(TRes);
                    }
                    else return (TRes) res;
                }

                public RowContract InsertRet<SubRowContract>(SubRowContract OneValue) => InsertRet((IEnumerable<SubRowContract>)(new SubRowContract[] { OneValue })).Single();

                public IEnumerable<RowContract> InsertRet<SubRowContract>(IEnumerable<SubRowContract> Collection)
                {
                    if (Collection.Any())
                    {
        #warning cache this
                        var SubRowColumns = RowContractInfo<SubRowContract>.Columns.Select(c => c.FieldName).ToArray();
                        var Query = new InsertQuery(Name, SubRowColumns, new Values(Collection.Select(RowContractDecomposer<SubRowContract>.Decompose)), Star.Instance);
                        var SQL = Query.Query.Build(Context.QueryContext);
                        return Context.ExecuteContract<RowContract>(SQL);
                    }
                    else return new RowContract[0];
                }

                public IEnumerable<RowContract> InsertRet<SubRowContract>(IQueryable<SubRowContract> Source)
                {
                    var row_source = Source as RowSource;
                    if (row_source != null)
                    {
        #warning cache this
                        var SubRowColumns = RowContractInfo<SubRowContract>.Columns.Select(c => c.FieldName).ToArray();
                        var Query = new InsertQuery(Name, SubRowColumns, row_source.theQuery.NewSelect(SubRowColumns), Star.Instance);
                        var SQL = Query.Query.Build(Context.QueryContext);
                        return Context.ExecuteContract<RowContract>(SQL);
                    }
                    else return InsertRet(Source.AsEnumerable());
                }

                public void Insert<SubRowContract>(IEnumerable<SubRowContract> Collection)
                {
                    if (Collection.Any())
                    {
        #warning cache this
                        var SubRowColumns = RowContractInfo<SubRowContract>.Columns.Select(c => c.FieldName).ToArray();
                        var Query = new InsertQuery(Name, SubRowColumns, new Values(Collection.Select(RowContractDecomposer<SubRowContract>.Decompose)), null);
                        var SQL = Query.Query.Build(Context.QueryContext);
                        Context.ExecuteNonQuery(SQL);
                    }
                }

                public void Insert<SubRowContract>(IQueryable<SubRowContract> Source)
                {
                    var row_source = Source as RowSource;
                    if (row_source != null)
                    {
        #warning cache this
                        var SubRowColumns = RowContractInfo<SubRowContract>.Columns.Select(c => c.FieldName).ToArray();
                        var Query = new InsertQuery(Name, SubRowColumns, row_source.theQuery.NewSelect(SubRowColumns), null);
                        var SQL = Query.Query.Build(Context.QueryContext);
                        Context.ExecuteNonQuery(SQL);
                    }
                    else Insert(Source.AsEnumerable());
                }*/

        public int Delete(Expression<Func<RowContract, bool>> predicate)
        {
            var sql_predicate = Context.QueryContext.BuildPredicate(PreEvaluate(predicate), null, member =>
            {
#if DEBUG
                if (!Attribute.IsDefined(member, TypeOf.FieldAttribute)) throw new InvalidContractException(typeof(RowContract), "Field name is not defined.");
#endif
#warning не самый быстрый способ. не закешировать ли?
                return (Attribute.GetCustomAttribute(member, TypeOf.FieldAttribute) as FieldAttribute).ColumnName;
            });
            if (sql_predicate == null) throw new NotSupportedException("This predicate cannot be translated into an SQL code.");

            var SQL = new DeleteQuery(Name, string.Concat(sql_predicate)).Query.Build(Context.QueryContext);
            return Context.ExecuteNonQuery(SQL);
        }

        #region INSERT support

        private InsertQuery BuildInsertQuery(IEnumerable Rows, IColumnDefinion Output)
        {
            var enumerator = Rows.GetEnumerator();
            if (enumerator.MoveNext())
            {
                var ElementType = enumerator.Current.GetType();

                var ContractColumns = Columns.ToArray();
                var ElementProps = ElementType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).
                    Select(p => new
                    {
                        property = p,
                        column = ContractColumns.SingleOrDefault(col => col.ContractName == p.Name)
                    }).ToArray();

                if (ElementProps.Any(p => p.column == null)) throw new ContractMismatchException($"Table contract '{nameof(RowContract)}' does not contain these properties: {string.Join(", ", ElementProps.Where(p => p.column == null).Select(p => "'" + p.property.Name + "'"))}");

                var ElementColumns = ElementProps.Where(p => p.column != null).ToArray();
#warning Check types!

                var raw = new List<object[]>();
                do
                {
                    if (ElementType != enumerator.Current.GetType()) throw new ArgumentException("Inserted collection should contain elements of same type.");

                    raw.Add(ElementColumns.Select(c => c.property.GetValue(enumerator.Current)).ToArray());
                } while (enumerator.MoveNext());

                return new InsertQuery(Name, ElementColumns.Select(c => c.column.FieldName).ToArray(), new Values(raw), Output);
            }
            else return null;
        }

        void ITable<RowContract>.Insert(object OneValue) => ((ITable<RowContract>)this).Insert(new object[] { OneValue });

        void ITable<RowContract>.Insert(IEnumerable Collection)
        {
            var INSERT = BuildInsertQuery(Collection, null);
            Context.ExecuteNonQuery(INSERT.Query.Build(Context.QueryContext));

        }

        RowContract ITable<RowContract>.InsertRet(object OneValue) => ((ITable<RowContract>)this).InsertRet(new object[] { OneValue }).Single();

        IEnumerable<RowContract> ITable<RowContract>.InsertRet(IEnumerable Collection)
        {
            var INSERT = BuildInsertQuery(Collection, Star.Instance);
            return Context.ExecuteContract<RowContract>(INSERT.Query.Build(Context.QueryContext));
        }

        #endregion

        /// <summary> This constructor will be called dynamically </summary>
        internal Table(DatabaseContext ConnectionContext, QualifiedIdentifier TableName)
            : base(ConnectionContext, new SelectQuery(TableName, RowContractInfo<RowContract>.Columns.Select(c=>c.FieldName).ToArray()))
        {
            Name = TableName;            
        }
    }
}

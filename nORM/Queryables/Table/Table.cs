﻿using ExpLess;
using MakeSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static ExpLess.PartialEvaluator;

namespace nORM
{
    internal class Table<RowContract> : RowSource<RowContract>, ITable<RowContract>
    {
        /// <summary> Collection of columns of the table </summary>
        public IReadOnlyList<DataColumn> Columns => RowContractInfo<RowContract>.Columns;

        /// <summary> Gets a name of the table, based on contract declaration. </summary>
        internal QualifiedIdentifier Name { get; }


        [Obsolete("will be removed")]
        public IEnumerable<RowContract> InsertRet<SubRowContract>(IQueryable<SubRowContract> Source)
        {
            var row_source = Source as RowSource;
            if (row_source != null)
            {
#warning cache this
                var SubRowColumns = RowContractInfo<SubRowContract>.Columns.Select(c => c.FieldName).ToArray();
                var Query = new InsertQuery(Name, SubRowColumns, row_source.theQuery.NewSelect(SubRowColumns), Star.Instance);
                var SQL = Query.Query.Build(Context.QueryContext);
                var res = Context.ExecuteContract<RowContract>(SQL);
                while (res.MoveNext()) yield return res.Current;
            }
            else {
                var res = ((ITable<RowContract>)this).InsertRet(Source.AsEnumerable());
                foreach (var r in res) yield return r;
            }
        }

        [Obsolete("will be removed")]
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
            else ((ITable<RowContract>)this).Insert(Source.AsEnumerable());
        }

        public int Delete(Expression<Func<RowContract, bool>> predicate)
        {
            var sql_predicate = Context.QueryContext.BuildPredicate(new DiscriminatedExpression(predicate).Minimized.Expression, null, member =>
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
            var res = Context.ExecuteContract<RowContract>(INSERT.Query.Build(Context.QueryContext));
            while (res.MoveNext()) yield return res.Current;
        }

        #endregion

        void ITable<RowContract>.Update(Expression<Func<RowContract, bool>> predicate, Expression<Func<RowContract, object>> transformation)
        {
            // predicate 

            var sql_predicate = Context.QueryContext.BuildPredicate(new DiscriminatedExpression(predicate).Minimized.Expression, null, member =>
            {
#if DEBUG
                if (!Attribute.IsDefined(member, TypeOf.FieldAttribute)) throw new InvalidContractException(typeof(RowContract), "Field name is not defined.");
#endif
#warning не самый быстрый способ. не закешировать ли?
                return (Attribute.GetCustomAttribute(member, TypeOf.FieldAttribute) as FieldAttribute).ColumnName;
            });
            if (sql_predicate == null) throw new NotSupportedException("This predicate cannot be translated into an SQL code.");

            // set clause

            var Fields = new List<Tuple<string, Expression>>();
            ParameterExpression input = null;

            if (transformation.NodeType == ExpressionType.Lambda)
            {
                var body = (transformation as LambdaExpression).Body;
                input = (transformation as LambdaExpression).Parameters[0];
                if (body.NodeType == ExpressionType.New)
                {
                    var New = body as NewExpression;

                    for (int i = 0; i < New.Members.Count; i++)
                        Fields.Add(new Tuple<string, Expression>(New.Members[i].Name, New.Arguments[i]));
                }
                else throw new NotSupportedException("UPDATE Lambda must be a 'new' expression.");
            }
            else throw new NotSupportedException("UPDATE Expression must be a Lambda expression.");

            var Set = new List<Tuple<LocalIdentifier, string[]>>();

            foreach (var f in Fields)
            {
                if (!Columns.Any(c => c.ContractName == f.Item1))
                    throw new ContractMismatchException($"Field '{f.Item1}' does not exist in the contract ('{nameof(RowContract)}').");

                var sql_setter = Context.QueryContext.BuildPredicate(new DiscriminatedExpression(f.Item2).Minimized.Expression, input, member =>
                {
#if DEBUG
                    if (!Attribute.IsDefined(member, TypeOf.FieldAttribute)) throw new InvalidContractException(typeof(RowContract), "Field name is not defined.");
#endif
#warning не самый быстрый способ. не закешировать ли?
                    return (Attribute.GetCustomAttribute(member, TypeOf.FieldAttribute) as FieldAttribute).ColumnName;
                });
                if (sql_setter == null) throw new NotSupportedException($"Expression '{f.Item2}' cannot be translated into a valid SQL code.");

#warning Check contract types
#warning concat
                Set.Add(new Tuple<LocalIdentifier, string[]>(Columns.Single(c => c.ContractName == f.Item1).FieldName, sql_setter));
            }

            var UPDATE = new UpdateQuery(Name, string.Concat(sql_predicate), Set);
            Context.ExecuteNonQuery(UPDATE.Query.Build(Context.QueryContext));
        }


        /// <summary> This constructor will be called dynamically </summary>
        internal Table(DatabaseContext ConnectionContext, QualifiedIdentifier TableName)
            : base(ConnectionContext, new SelectQuery(TableName, RowContractInfo<RowContract>.Columns.Select(c => c.FieldName).ToArray()))
        {
            Name = TableName;
        }
    }

}
using MakeSQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace nORM
{
    /// <summary>
    /// The basic table contract. 
    /// Can be either used in a database contract directly 
    /// or extended to provide additional functionality.
    /// </summary>
    public interface ITable<RowContract> : IQueryable<RowContract>
    {
        /// <summary> Gets a name of the table, based on contract declaration. </summary>
        QualifiedIdentifier Name { get; }

        /// <summary> Collection of columns of the table </summary>
        IReadOnlyList<DataColumn> Columns { get; }

        /// <summary> Deletes rows from the table, based on a predicate. Returns a number of the affected rows. </summary>
        int Delete(Expression<Func<RowContract, bool>> predicate);
    }

    /// <summary>
    /// Extension to the table contract.
    /// Used to define a field subset which can be used in the INSERT statement.
    /// </summary>
    /// <typeparam name="RowSubcontract"> A field subset without primary keys and evaluated columns. </typeparam>
    public interface IInsertable<RowSubcontract>
    {
        /// <summary>
        /// A single INSERT query
        /// </summary>
        /// <param name="Row"> A single row to be inserted </param>
        void Insert(RowSubcontract Row);

        /// <summary>
        /// An INSERT query with table constructor 
        /// </summary>
        /// <param name="Rows"> A collection of rows to insert </param>
        void Insert(IEnumerable<RowSubcontract> Rows);

        /// <summary>
        /// An INSERT operation with subquery used as a source.
        /// </summary>
        /// <param name="Source"> A subquery to select source rows </param>
        void Insert(IQueryable<RowSubcontract> Source);
    }
}
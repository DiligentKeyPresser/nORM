﻿using System;
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
        /// <summary> Collection of columns of the table </summary>
        IReadOnlyList<DataColumn> Columns { get; }

        /// <summary> Deletes rows from the table, based on a predicate. Returns a number of the affected rows. </summary>
        int Delete(Expression<Func<RowContract, bool>> predicate);

        /// <summary>
        /// A single INSERT query
        /// </summary>
        /// <param name="OneValue"> A single row to be inserted </param>
        void Insert<SubRowContract>(SubRowContract OneValue);

        /// <summary>
        /// A single INSERT query.
        /// Returns a value of requered column.
        /// </summary>
        /// <param name="OneValue"> A single row to be inserted </param>
        /// <param name="ReturningColumn"> A column to be returned </param>
        TRes InsertReturning<SubRowContract, TRes>(SubRowContract OneValue, DataColumn ReturningColumn);

        /// <summary>
        /// An INSERT query with table constructor 
        /// </summary>
        /// <param name="Collection"> A collection of rows to insert </param>
        void Insert<SubRowContract>(IEnumerable<SubRowContract> Collection);

        /// <summary>
        /// An INSERT operation with subquery used as a source.
        /// </summary>
        /// <param name="Source"> A subquery to select source rows </param>
        void Insert<SubRowContract>(IQueryable<SubRowContract> Source);
    }
}
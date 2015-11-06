using nORM.SQL;
using System;
using System.Collections.Generic;

namespace nORM
{
    /// <summary>
    /// Connectors must establish a connection to the database.
    /// They also help to choose an SQL constructor for different database engines.
    /// </summary>
    public abstract class Connector
    {
        /// <summary>
        /// Establish a new connection, execute query and return the first cell of the result set.
        /// </summary>
        internal abstract object ExecuteScalar(string Query);

        /// <summary>
        /// Establish a new connection, execute the query and refurn the full result set.
        /// Each result row will be transformed into a TElement by the given `Projection`.
        /// </summary>
#warning IEnumerator would be better
        internal abstract IEnumerable<TElement> ExecuteProjection<TElement>(string Query, Func<object[], TElement> Projection);

        /// <summary>
        /// Return Query Factory apropriate for the database engine.
        /// </summary>
        internal abstract IQueryFactory GetQueryFactory();
    }
}
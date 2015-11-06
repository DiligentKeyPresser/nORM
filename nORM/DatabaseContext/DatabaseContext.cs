using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace nORM
{
    /// <summary> Обработчик события создания текста SQL команды. Предназначается для мониторинга работы оболочки. </summary>
    /// <param name="CommandText"> Текст созданной команды </param>
    public delegate void BasicCommandHandler(string CommandText);

    /// <summary>
    /// IDatabase implementation.
    /// User - defined Database Contract extends this class by Reflection.Emit.
    /// Uses given `Connector`s to establish a connection to the database and to choose SQL constructor.
    /// </summary>
    internal abstract class DatabaseContext : IDatabase
    {
        // We dont want to make it public and send commands directly.
        // Routing via DatabaseContext will enable monitoring and profiling features in future. 
        private readonly Connector connection;

        public event BasicCommandHandler BeforeCommandExecute;

        internal DatabaseContext(Connector Connection) { connection = Connection; }

        /// <summary>
        /// Delegates query execution to the underlying connector.
        /// </summary>
        internal object ExecuteScalar(string Query)
        {
            if (BeforeCommandExecute != null) BeforeCommandExecute(Query);
            return connection.ExecuteScalar(Query);
        }

        /// <summary>
        /// Delegates query execution to the underlying connector.
        /// Each result row will be transformed into a TElement by the given `Projection`.
        /// </summary>
        internal IEnumerable<TElement> ExecuteProjection<TElement>(string Query, Func<object[], TElement> Projection)
        {
            if (BeforeCommandExecute != null) BeforeCommandExecute(Query);
            return connection.ExecuteProjection(Query, Projection);
        }

        /// <summary>
        /// Delegates query execution to the underlying connector.
        /// Each result row will be transformed into a row contract instance.
        /// </summary>
#warning IEnumerator would be better
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IEnumerable<RowContract> ExecuteContract<RowContract>(string Query) => ExecuteProjection(Query, RowContractInflater<RowContract>.Inflate);
    }

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
    }
}
﻿using MakeSQL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Threading;

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
        /// Establish a new connection and execute a command against it.
        /// </summary>
        internal abstract int ExecureNonQuery(string Command);

        /// <summary>
        /// Establish a new connection, execute the query and refurn the full result set.
        /// Each result row will be transformed into a TElement by the given `Projection`.
        /// </summary>
        internal abstract IEnumerator<TElement> ExecuteProjection<TElement>(string Query, Func<object[], TElement> Projection);

        /// <summary>
        /// Return Query Factory apropriate for the database engine.
        /// </summary>
        internal abstract SQLContext GetQueryFactory();

        // cant inherit this class outside the library
        internal Connector() { }
    }

    /// <summary>
    /// Connection to a remote database
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class NetworkConnector : Connector
    {
        /// <summary>
        /// Server name
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Database name
        /// </summary>
        public string Database { get; }

        /// <summary>
        /// The connection string
        /// </summary>
        protected readonly string ConnectionString;

        protected NetworkConnector(string host, string database, string connection_string)
        {
            Host = host;
            Database = database;
            ConnectionString = connection_string;
        }
    }

    /// <summary>
    /// Cjnnection to a remote database via standart driver
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class DBNetworkConnector : NetworkConnector
    {
        protected DBNetworkConnector(string host, string database, string connection_string)
            : base(host, database, connection_string)
        { }

        protected abstract IDbConnection MakeConnection();
        protected abstract IDbCommand MakeCommand(string Text, IDbConnection Connection);

        internal override object ExecuteScalar(string Query)
        {
            using (var connection = MakeConnection())
            using (var command = MakeCommand(Query, connection))
            {
                connection.Open();
                return command.ExecuteScalar();
            }
        }

        internal override int ExecureNonQuery(string Command)
        {
            using (var connection = MakeConnection())
            using (var command = MakeCommand(Command, connection))
            {
                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

#if DEBUG
        private class DebugDisposeNotifier : IDisposable
        {
            private static int N = 0;
            private readonly int Current = Interlocked.Increment(ref N);
            public DebugDisposeNotifier() { Debug.WriteLine($"connection {Current} has been created."); }
            public void Dispose() => Debug.WriteLine($"connection {Current} has been disposed");
        }
#endif

        internal override IEnumerator<TElement> ExecuteProjection<TElement>(string Query, Func<object[], TElement> Projection)
        {
            using (var connection = MakeConnection())
            using (var command = MakeCommand(Query, connection))
#if DEBUG
            using (new DebugDisposeNotifier())
#endif
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    int ColumnCount = reader.FieldCount;
                    var row = new object[ColumnCount];

                    while (reader.Read())
                    {
                        reader.GetValues(row);
                        yield return Projection(row);
                    }
                }
            }
        }
    }
}
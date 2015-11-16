using MakeSQL;
using nORM.SQL;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;

namespace nORM
{
    /// <summary>
    /// Represents a connection to an SQL Server database.
    /// </summary>
    public sealed class PostgreSQLConnector : DBNetworkConnector
    {
        public PostgreSQLConnector(string host, string database, string user, string password)
            : base(host, database, $"Host={host};Database={database};Username={user};Password={password};")
        { }

        protected override IDbCommand MakeCommand(string Text, IDbConnection Connection) => new NpgsqlCommand(Text, Connection as NpgsqlConnection);

        protected override IDbConnection MakeConnection() => new NpgsqlConnection(ConnectionString);

        internal override SQLContext GetQueryFactory() => PostgreSQLContext.Singleton;
    }
}
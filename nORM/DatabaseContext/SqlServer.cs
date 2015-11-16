using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MakeSQL;

namespace nORM
{
    /// <summary>
    /// Represents a connection to an SQL Server database.
    /// </summary>
    public sealed class SqlServerConnector : DBNetworkConnector
    {
        public SqlServerConnector(string host, string database, string user, string password)
            : base(host, database, $"Data Source={host};Initial Catalog={database};Persist Security Info=True;User ID={user};Password={password};")
        { }

        protected override IDbCommand MakeCommand(string Text, IDbConnection Connection) => new SqlCommand(Text, Connection as SqlConnection);

        protected override IDbConnection MakeConnection() => new SqlConnection(ConnectionString);

        internal override SQLContext GetQueryFactory() => TSQLContext.Singleton;
    }
}
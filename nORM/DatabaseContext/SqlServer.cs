using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using nORM.SQL;

namespace nORM
{
    /// <summary>
    /// Represents a connection to an SQL Server database.
    /// </summary>
    public sealed class SqlServerConnector : Connector
    {
        public string Host { get; }

        public string Database { get; }

        private readonly string ConnectionString;

        public SqlServerConnector(string host, string database, string user, string password)
        {
            Host = host;
            Database = database;
            ConnectionString = string.Format("Data Source={0};Initial Catalog={1};Persist Security Info=True;User ID={2};Password={3};", host, database, user, password);
        }

        internal override object ExecuteScalar(string Query)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(Query, connection))
            {
                connection.Open();
                return command.ExecuteScalar();
            }
        }

        internal override IEnumerable<TElement> ExecuteProjection<TElement>(string Query, Func<object[], TElement> Projection)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(Query, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    int ColumnCount = reader.FieldCount;
                    var row = new object[ColumnCount];

                    // список нужен, иначе будет возвращаться итератор по уничтоженному IDisposable
                    var result = new List<TElement>();
#warning нельзя ли заранее узнать размер?
                    while (reader.Read())
                    {
                        reader.GetValues(row);
                        result.Add(Projection(row));
                    }
                    return result;
                }
            }
        }

        internal override IQueryFactory GetQueryFactory() => TSQLQueryFactory.Singleton;
    }
}
using nORM.SQL;
using Npgsql;
using System;
using System.Collections.Generic;

namespace nORM
{
    /// <summary>
    /// Represents a connection to an SQL Server database.
    /// </summary>
    public sealed class PostgreSQLConnector : NetworkConnector
    {
        public PostgreSQLConnector(string host, string database, string user, string password)
            : base(host, database, $"Host={host};Database={database};Username={user};Password={password};")
        { }

        internal override object ExecuteScalar(string Query)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            using (var command = new NpgsqlCommand(Query, connection))
            {
                connection.Open();
                return command.ExecuteScalar();
            }
        }

        internal override IEnumerable<TElement> ExecuteProjection<TElement>(string Query, Func<object[], TElement> Projection)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            using (var command = new NpgsqlCommand(Query, connection))
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

        internal override IQueryFactory GetQueryFactory() => PostgreSQLQueryFactory.Singleton;
    }
}
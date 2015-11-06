using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;

namespace nORM
{
    /// <summary> Обработчик события создания текста SQL команды. Предназначается для мониторинга работы оболочки. </summary>
    /// <param name="CommandText"> Текст созданной команды </param>
    public delegate void BasicCommandHandler(string CommandText);

    internal abstract class DatabaseContext : IDatabase
    {
        public event BasicCommandHandler BeforeCommandExecute;

        private ConnectionProvider connection;

        internal DatabaseContext(ConnectionProvider Connection) { connection = Connection; }

        internal object ExecuteScalar(string Query)
        {
            if (BeforeCommandExecute != null) BeforeCommandExecute(Query);
            return connection.ExecuteScalar(Query);
        }

        internal IEnumerable<TElement> ExecuteProjection<TElement>(string Query, Func<object[], TElement> Projection)
        {
            if (BeforeCommandExecute != null) BeforeCommandExecute(Query);
            return connection.ExecuteProjection(Query, Projection);
        }

#warning IEnumerator would be better
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IEnumerable<RowContract> ExecuteContract<RowContract>(string Query) => ExecuteProjection(Query, RowContractInflater<RowContract>.Inflate);
    }

    public abstract class ConnectionProvider
    {
        internal abstract object ExecuteScalar(string Query);

#warning IEnumerator would be better
        internal abstract IEnumerable<TElement> ExecuteProjection<TElement>(string Query, Func<object[], TElement> Projection);
    }

    public sealed class SqlServerConnectionProvider : ConnectionProvider
    {
        public string Host { get; }

        public string Database { get; }

        private readonly string ConnectionString;

        public SqlServerConnectionProvider(string host, string database, string user, string password)
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
    }

}
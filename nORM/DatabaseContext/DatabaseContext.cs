using MakeSQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace nORM
{
    /// <summary>
    /// The most basic IDatabase implementation.
    /// User-defined Database Contract extends this class by Reflection.Emit.
    /// Uses given `Connector` to establish a connection to the database and to choose SQL constructor.
    /// </summary>
    internal abstract class DatabaseContext : IDatabase
    {
        /// <summary> SQL command sink. </summary>
        private readonly Connector connection;

        /// <summary> This object helps to produce actual sql commands for the current server. </summary>
        internal SQLContext QueryContext { get; }

        /// <summary> SQL command notifier. </summary>
        public event BasicCommandHandler BeforeCommandExecute;

        internal DatabaseContext(Connector Connection)
        {
            connection = Connection;
            QueryContext = Connection.GetQueryFactory();
        }

        /// <summary> Delegates query execution to the underlying connector. </summary>
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

    /// <summary> Introduces initializer for the table fields. </summary>
    internal abstract class DatabaseContext<DatabaseContract> : DatabaseContext
    {
#warning move comewhere else?
        /// <summary> All the tables of the database contract. </summary>
        private static readonly PropertyInfo[] contract_tables = typeof(DatabaseContract).GetProperties().Where(prop => Attribute.IsDefined(prop, TypeOf.TableAttribute)).ToArray();

        internal DatabaseContext(Connector Connection)
            :base (Connection)
        {
            foreach (var TableProperty in Tables)
            {
                var TAttr = Attribute.GetCustomAttribute(TableProperty, TypeOf.TableAttribute) as TableAttribute;

                var TableType = TableProperty.PropertyType;

                var ITableInterface = TableContractHelpers.ExtractBasicTableInterface(TableType);
#warning move outside
                if (ITableInterface == null) throw new InvalidContractException(TableType, "table contract must be an interface of ITable<>");

                var RowContract = ITableInterface.GetGenericArguments()[0];

#warning method name
                var Tableconstructor = TypeOf.TableContractInflater.MakeGenericType(TableType, RowContract).GetMethod("Inflate", BindingFlags.Public | BindingFlags.Static);

                var TableField = GetType().GetField("__table_" + TableProperty.Name, BindingFlags.NonPublic | BindingFlags.Instance);

                TableField.SetValue(this, Tableconstructor.Invoke(null, new object[] { this, TableAttribute.extract_name_from_property(TableProperty) }));
            }
        }

#warning move comewhere else?
        /// <summary> Gets all the tables from the database contract. </summary>
        internal static IEnumerable<PropertyInfo> Tables => contract_tables;
    }
}
using MakeSQL;
using System;
using System.Reflection;

namespace nORM
{
    /// <summary> Table column metadata object. </summary>
    public sealed class DataColumn
    {
#warning validation only. make separate class for the validation and wipe this prop out
        [Obsolete("Will be removed")]
        /// <summary> Corresponding contract property </summary>
        internal PropertyInfo PropertyMetadata { get; }

        /// <summary> Type of the column data </summary>
        public Type ColumnType { get; }

        /// <summary> Name of the field in a database. </summary>
        public LocalIdentifier FieldName { get; }

        /// <summary> Name of the property in a row contract </summary>
        public string ContractName { get; }

        public DataColumn(PropertyInfo prop)
        {
            PropertyMetadata = prop;

            ColumnType = prop.PropertyType;
            ContractName = prop.Name;

            var Attr = Attribute.GetCustomAttribute(prop, TypeOf.FieldAttribute) as FieldAttribute;
            FieldName = Attr.ColumnName;
        }
    }
}
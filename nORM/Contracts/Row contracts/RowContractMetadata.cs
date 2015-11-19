using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace nORM
{
    /// <summary> Provides a row contract metadata. </summary>
    internal static class RowContractInfo<RowContract>
    {
        /// <summary> Gets the fields defined in the subcontract </summary>
        private static IEnumerable<PropertyInfo> GetFields(Type ContractType)
        {
            foreach (var f in ContractType.GetProperties().Where(prop => Attribute.IsDefined(prop, TypeOf.FieldAttribute))) yield return f;
            foreach (var s in ContractType.GetInterfaces())
                foreach (var f in GetFields(s)) yield return f;
        }

        /// <summary> Gets a collection of fields in the contract. </summary>
        public static IReadOnlyList<DataColumn> Columns { get; } = GetFields(typeof(RowContract)).Select(field => new DataColumn(field)).ToList().AsReadOnly();
    }
}
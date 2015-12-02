using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace nORM
{
#warning SqlException is the only way to get warned 
    public sealed class Row : DynamicObject
    {
        private readonly object dataContainer;

        private Row(object rowPresenter) { dataContainer = rowPresenter; }

        public static dynamic From(object o) => new Row(o);

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
#warning can be slooooooooooooooooow
#warning should throw an exception if RowType is not a row contract

            var RowType = binder.ReturnType;

            var data_columns_getter = typeof(RowContractInfo<>).MakeGenericType(RowType).GetProperty("Columns", BindingFlags.Public | BindingFlags.Static);
            var data_columns = data_columns_getter.GetValue(null) as IReadOnlyList<DataColumn>;
            var data = data_columns.Select(col =>
            {
                var prop = dataContainer.GetType().GetProperty(col.ContractName);
                return prop.GetValue(dataContainer);
            }).ToArray();

            var inflater = typeof(RowContractInflater<>).MakeGenericType(RowType).GetMethod("Inflate", BindingFlags.Public | BindingFlags.Static);
            result = inflater.Invoke(null, new object[] { data });
            return true;
        }
    }
}

using System.Linq;

namespace nORM
{
    internal static class RowContractDecomposer<RowContract>
    {
        public static object[] Decompose(RowContract Row)
        {
#warning just read fields one by one if `Row` is an inflated row conract
            return RowContractInfo<RowContract>.Columns.Select(c => c.PropertyMetadata.GetValue(Row)).ToArray();
        }
    }
}
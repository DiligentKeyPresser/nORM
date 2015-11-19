using System.Collections.Generic;
using System.Linq;

namespace MakeSQL
{
    public sealed class Values : IInsertSource
    {
        public Builder InsertSourceDefinion { get; }
        private readonly IEnumerable<IEnumerable<IUnnamedColumnDefinion>> data;

#warning add overload with IEnumerable<IUnnamedColumnDefinion[]>
        public Values(IEnumerable<object[]> rows)
        {
            InsertSourceDefinion = new Builder(Compile);
            data = rows.Select(row=>row.Select(cell => new Constant(cell)));
        }

        private IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return "VALUES ";
            bool firstrow = true;
            foreach (var row in data)
            {
#if DEBUG
                yield return "\r\n   ";
#endif
                if (!firstrow) yield return ", ";
                else firstrow = false;
                
                yield return "(";

                bool firstcell = true;
                foreach (var cell in row)
                {
                    if (!firstcell) yield return ", ";
                    else firstcell = false;

                    var cellbuilder = cell.ColumnDefinion.Compile(LanguageContext);
                    while (cellbuilder.MoveNext()) yield return cellbuilder.Current;
                }

                yield return ")";
            }
        }
    }
}
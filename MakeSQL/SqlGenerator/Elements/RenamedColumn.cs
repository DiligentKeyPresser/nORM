using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class RenamedColumn : IColumnDefinion
    {
        private readonly IUnnamedColumnDefinion baseColumn;

        private readonly LocalIdentifier AS;

        public Builder ColumnDefinion => baseColumn.ColumnDefinion;

        public Builder NamedColumnDefinion { get; }
        
        internal RenamedColumn(IUnnamedColumnDefinion Base, LocalIdentifier Alias)
        {
            if (Alias == null) throw new ArgumentNullException("Name", "Subquery must have a name.");
            baseColumn = Base;
            AS = Alias;
            NamedColumnDefinion = new Builder(Compile);
        }

        private IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return "(";

            var subquery = baseColumn.ColumnDefinion.Compile(LanguageContext);
            while (subquery.MoveNext()) yield return subquery.Current;

            yield return ") AS ";

            var name = AS.ColumnDefinion.Compile(LanguageContext);
            while (name.MoveNext()) yield return name.Current;
        }
    }
}
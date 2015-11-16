using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class RenamedColumn : Buildable, IColumnDefinion
    {
        private readonly IUnnamedColumnDefinion baseColumn;

        private readonly LocalIdentifier AS;

        Buildable IColumnDefinion.Definion => this;

        Buildable IUnnamedColumnDefinion.Definion => baseColumn.Definion;

        internal RenamedColumn(IUnnamedColumnDefinion Base, LocalIdentifier Alias)
        {
            if (Alias == null) throw new ArgumentNullException("Name", "Subquery must have a name.");
            baseColumn = Base;
            AS = Alias;
        }

        internal override IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return "(";

            var subquery = baseColumn.Definion.Compile(LanguageContext);
            while (subquery.MoveNext()) yield return subquery.Current;

            yield return ") AS ";

            var name = AS.Compile(LanguageContext);
            while (name.MoveNext()) yield return name.Current;
        }
    }

    public static class ColumnExtensions
    {
        public static RenamedColumn AS(this IUnnamedColumnDefinion Self, LocalIdentifier Alias) => new RenamedColumn(Self, Alias);
    }
}
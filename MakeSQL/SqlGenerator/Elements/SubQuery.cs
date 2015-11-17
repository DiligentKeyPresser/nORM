using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class SubQuery : ISelectSource
    {
        private readonly SelectQuery baseQuery;

        private readonly LocalIdentifier AS;

        public Builder SourceDefinion { get; }

        internal SubQuery(SelectQuery Base, LocalIdentifier Alias)
        {
            if (Alias == null) throw new ArgumentNullException("Name", "Subquery must have a name.");
            baseQuery = Base;
            AS = Alias;
            SourceDefinion = new Builder(Compile);
        }

        private IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return "(";
#if DEBUG
            yield return "\r\n ";
#endif

            var subquery = baseQuery.Query.Compile(LanguageContext);
            while (subquery.MoveNext()) yield return subquery.Current;

#if DEBUG
            yield return "\r\n";
#endif
            yield return ") AS ";

            var name = AS.ColumnDefinion.Compile(LanguageContext);
            while (name.MoveNext()) yield return name.Current;
        }
    }
}
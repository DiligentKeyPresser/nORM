using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class SubQuery : Buildable, ISelectSource
    {
        private readonly SelectQuery baseQuery;

        private readonly LocalIdentifier AS;

        public Buildable Definion => this;

        internal SubQuery(SelectQuery Base, LocalIdentifier Alias)
        {
            if (Alias == null) throw new ArgumentNullException("Name", "Subquery must have a name.");
            baseQuery = Base;
            AS = Alias;
        }

        internal override IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return "(";

            var subquery = baseQuery.Compile(LanguageContext);
            while (subquery.MoveNext()) yield return subquery.Current;

            yield return ") AS ";

            var name = AS.Compile(LanguageContext);
            while (name.MoveNext()) yield return name.Current;
        }
    }

    public static class SubQueryExtensions
    {
        public static SubQuery AS(this SelectQuery Self, LocalIdentifier Alias) => new SubQuery(Self, Alias);
    }
}
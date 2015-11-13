using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class SubQuery : Internals.Builder, ISelectSource
    {
        private readonly ISelectQuery baseQuery;

        private readonly LocalIdentifier AS;

        internal SubQuery(ISelectQuery Base, LocalIdentifier Name)
        {
            if (AS == null) throw new ArgumentNullException("Name", "Subquery must have a name.");
            baseQuery = Base;
            AS = Name;
        }

        internal override IEnumerator<string> Compile(QueryFactory LanguageContext)
        {
            yield return "(";

            var subquery = baseQuery.Builder.Compile(LanguageContext);
            while (subquery.MoveNext()) yield return subquery.Current;

            yield return ") AS ";

            var name = AS.Compile(LanguageContext);
            while (subquery.MoveNext()) yield return subquery.Current;
        }
    }

    public static class SubQueryExtensions
    {
        public static SubQuery AS(this ISelectQuery Self, LocalIdentifier Name) => new SubQuery(Self, Name);
    }
}
using System;
using System.Linq.Expressions;
using System.Text;

namespace nORM
{
    namespace SQL
    {
        [Obsolete("old", true)]
        internal sealed class TSQLQueryFactory : StandartSQLQueryFactory
        {
            #region Singleton
            private TSQLQueryFactory() { }
            public static TSQLQueryFactory Singleton { get; } = new TSQLQueryFactory();
            #endregion

            public override SelectQuery Select(string source, string[] fields, string SourceAlias) => new TSQLSelectQuery(source, fields, SourceAlias);

            public override string EscapeIdentifier(string schema, string name)
            {
                // SQL Server supports standart escaping, but this looks better :)
                var builder = new StringBuilder();

                builder.Append("[");
                if (!string.IsNullOrEmpty(schema))
                {
                    builder.Append(schema);
                    builder.Append("].[");
                }
                builder.Append(name);
                builder.Append("]");

                return builder.ToString();
            }
        }

        [Obsolete("old", true)]
        internal sealed class TSQLSelectQuery : StandartSQLSelectQuery
        {
            private static readonly string[] TSQLLongCountClause = new string[] { "COUNT_BIG(*)" };
            private static readonly string[] TSQLAnySelectClause = new string[] { "TOP 1 1 AS P" };
            private static readonly string[] TSQLAnySuperquery = new string[] { "CAST(COUNT(*) AS BIT)" };

            protected override SelectQuery Clone() => new TSQLSelectQuery(source, fields, source_alias) { where = where };

            internal TSQLSelectQuery(string source, string[] fields, string SourceAlias)
                : base(source, fields, SourceAlias)
            { }

            public override SelectQuery MakeLongCount()
            {
                var clone = Clone() as TSQLSelectQuery;
                clone.fields = TSQLLongCountClause;
                return clone;
            }

            public override SelectQuery MakeAny()
            {
#warning interference with TOP statement
                var clone = Clone() as TSQLSelectQuery;
                clone.fields = TSQLAnySelectClause;
                return new TSQLSelectQuery(clone.ToString(), TSQLAnySuperquery, "T");
            }
        }
    }
}
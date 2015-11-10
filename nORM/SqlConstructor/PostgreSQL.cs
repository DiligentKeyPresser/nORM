using System;
using System.Linq.Expressions;
using System.Text;

namespace nORM
{
    namespace SQL
    {
        internal sealed class PostgreSQLQueryFactory : StandartSQLQueryFactory
        {
            #region Singleton
            private PostgreSQLQueryFactory() { }
            public static PostgreSQLQueryFactory Singleton { get; } = new PostgreSQLQueryFactory();
            #endregion

            public override SelectQuery Select(string source, string[] fields, string SourceAlias) => new PostgreSQLSelectQuery(source, fields, SourceAlias);
        }

        internal sealed class PostgreSQLSelectQuery : StandartSQLSelectQuery
        {
            private static readonly string[] PostgreSQLAnySelectClause = new string[] { "TOP 1 1 AS P" };
            private static readonly string[] PostgreSQLAnySuperquery = new string[] { "CAST(COUNT(*) AS BOOLEAN)" };

            protected override SelectQuery Clone() => new PostgreSQLSelectQuery(source, fields, source_alias) { where = where };

            internal PostgreSQLSelectQuery(string source, string[] fields, string SourceAlias)
                : base(source, fields, SourceAlias)
            { }

            public override SelectQuery MakeAny()
            {
#warning interference with TOP statement
                var clone = Clone() as PostgreSQLSelectQuery;
                clone.fields = PostgreSQLAnySelectClause;
                return new TSQLSelectQuery(clone.ToString(), PostgreSQLAnySuperquery, "T");
            }
        }
    }
}
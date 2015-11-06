using System;
using System.Text;

namespace nORM
{
    namespace SQL
    {
        internal sealed class TSQLQueryFactory : IQueryFactory
        {
            #region Singleton
            private TSQLQueryFactory() { }
            public static TSQLQueryFactory Singleton { get; } = new TSQLQueryFactory();
            #endregion

            public SelectQuery Select(string source, string[] fields, string SourceAlias) => new TSQLSelectQuery(source, fields, SourceAlias);
        }

        internal sealed class TSQLSelectQuery : SelectQuery
        {
            private readonly string source;
            private readonly string source_alias;
            private string[] fields;
            private string[] where;

            protected override SelectQuery Clone() => new TSQLSelectQuery(source, fields, source_alias) { where = where };

            internal TSQLSelectQuery(string source, string[] fields, string SourceAlias)
            {
                this.source = source;
                this.fields = fields;
                where = new string[0];
                source_alias = SourceAlias;
            }

            public override SelectQuery Where(string clause)
            {
                var clone = Clone() as TSQLSelectQuery;

                var new_where = new string[where.Length + 1];
                Array.Copy(where, new_where, where.Length);
                new_where[where.Length] = clause;

                clone.where = new_where;
                return clone;
            }

            protected override string Build()
            {
                var builder = new StringBuilder();

                builder.Append("SELECT ");

                for (int i = 0; i < fields.Length; i++)
                {
                    builder.Append(fields[i]);
                    if (i < fields.Length - 1) builder.Append(", ");
                }

                builder.Append(" FROM ");

                if (source_alias != null) builder.Append("(");
                builder.Append(source);
                if (source_alias != null)
                {
                    builder.Append(") AS ");
                    builder.Append(source_alias);
                }

                if (where.Length > 0)
                {
                    builder.Append(" WHERE ");
                    bool brackets = where.Length > 1;

                    for (int i = 0; i < where.Length; i++)
                    {
                        if (brackets) builder.Append("(");
                        builder.Append(where[i]);
                        if (brackets) builder.Append(")");

                        if (i < where.Length - 1) builder.Append(" AND ");
                    }
                }

                return builder.ToString();
            }

            public override SelectQuery MakeCount()
            {
                var clone = Clone() as TSQLSelectQuery;
                clone.fields = new string[] { "COUNT(*)" };
                return clone;
            }

            public override SelectQuery MakeLongCount()
            {
                var clone = Clone() as TSQLSelectQuery;
                clone.fields = new string[] { "COUNT_BIG(*)" };
                return clone;
            }

            public override SelectQuery MakeAny()
            {
#warning interference with TOP statement
                var clone = Clone() as TSQLSelectQuery;
                clone.fields = new string[] { "TOP 1 1 AS P" };
                return new TSQLSelectQuery(clone.ToString(), new string[] { "CAST(COUNT(*) AS BIT)" }, "T");
            }
        }
    }
}
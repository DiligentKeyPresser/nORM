﻿using System;
using System.Text;

namespace nORM
{
    namespace SQL
    {
        internal sealed class TSQLSelectQuery : SelectQuery
        {
            private readonly string source;
            private readonly string source_alias;
            private string[] fields;
            private string[] where;

            public override SelectQuery Clone()
            {
                return new TSQLSelectQuery(source, fields, source_alias) { where = where };
            }

            public TSQLSelectQuery(string source, string[] fields, string SourceAlias)
            {
                this.source = source;
                this.fields = fields;
                where = new string[0];
                source_alias = SourceAlias;
            }

            public override void AddWhereClause(string clause)
            {
                ResetCache();

                var new_where = new string[where.Length + 1];
                Array.Copy(where, new_where, where.Length);
                new_where[where.Length] = clause;

                where = new_where; 
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

#warning the logic is a bit wierd
            public override void TurnIntoCount() { fields = new string[] { "COUNT(*)" }; }

            public override void TurnIntoLongCount() { fields = new string[] { "COUNT_BIG(*)" }; }

            public override SelectQuery MakeAny()
            {
                var ones = Clone() as TSQLSelectQuery;
                ones.fields = new string[] { "TOP 1 1 AS P" };
                return new TSQLSelectQuery(ones.ToString(), new string[] { "CAST(COUNT(*) AS BIT)" }, "T");
            }
        }
    }
}
using System;
using System.Text;

namespace nORM
{
    namespace SQL
    {
        internal sealed class TSQLSelectQuery : SelectQuery
        {
            private readonly string source;
            private string[] fields;
            private string[] where;

            public TSQLSelectQuery(string source, string[] fields)
            {
                this.source = source;
                this.fields = fields;
                where = new string[0];
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
                    builder.Append(i < fields.Length - 1 ? ", " : " ");
                }

                builder.Append("FROM ");

                builder.Append(source);

                if (where.Length > 0)
                {
                    builder.Append("WHERE ");
                    bool brackets = where.Length > 1;

                    for (int i = 0; i < where.Length; i++)
                    {
                        if (brackets) builder.Append("(");
                        builder.Append(fields[i]);
                        if (brackets) builder.Append(")");

                        builder.Append(i < fields.Length - 1 ? " AND " : " ");
                    }
                }

                return builder.ToString();
            }
        }
    }
}
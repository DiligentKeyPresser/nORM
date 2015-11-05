using System;
using System.Text;

namespace nORM
{
    namespace SQL
    {
        internal sealed class TSQLSelectQuery : SelectQuery
        {
            protected string[] fields;
            protected string source;

            public TSQLSelectQuery(string source, string[] fields)
            {
                this.source = source;
                this.fields = fields;
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

                return builder.ToString();
            }
        }
    }
}
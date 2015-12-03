using System;
using System.Collections.Generic;
using System.Linq;

namespace MakeSQL
{
    public sealed class UpdateQuery : IQuery
    {
        private QualifiedIdentifier Table;
#warning string???
        private readonly string where;
        private readonly IEnumerable<Tuple<LocalIdentifier, string[]>> SetClause;

        public Builder Query => new Builder(Compile);

        public UpdateQuery(QualifiedIdentifier Table, string where, IEnumerable<Tuple<LocalIdentifier, string[]>> SetClause)
        {
            this.Table = Table;
            this.where = where;
            if (!SetClause.Any()) throw new InvalidOperationException("UPDATE query must have at least one SET clause.");
            this.SetClause = SetClause;
        }

        private IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return "UPDATE ";

            var From = Table.SourceDefinion.Compile(LanguageContext);
            while (From.MoveNext()) yield return From.Current;

            yield return " SET ";
            bool first = true;
            foreach (var setter in SetClause)
            {
                if (!first) yield return ", ";
                else first = false;

                var field = setter.Item1.ColumnDefinion.Compile(LanguageContext);
                while (field.MoveNext()) yield return field.Current;

                yield return " = ";

                var value = setter.Item2;
                foreach (var s in value) yield return s;
            }

            yield return " WHERE ";
            yield return where;
        }
    }

}

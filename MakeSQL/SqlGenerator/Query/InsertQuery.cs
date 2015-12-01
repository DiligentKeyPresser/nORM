using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class InsertQuery : IQuery
    {
        private QualifiedIdentifier Into;
        private IColumnDefinion[] fields;
        private IInsertSource SourceData;
        private LocalIdentifier Output;

        // some methods like `Where` change state just after cloning, so we cannot assign the builder in a constructor
        public Builder Query => new Builder(Compile);

        public InsertQuery(QualifiedIdentifier Into, IColumnDefinion[] Fields, IInsertSource From, LocalIdentifier Output)
        {
            this.Into = Into;
            this.Output = Output;
            fields = Fields;
            SourceData = From;
        }

        private IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return "INSERT INTO ";

            var into = Into.SourceDefinion.Compile(LanguageContext);
            while (into.MoveNext()) yield return into.Current;

            yield return " (";
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i].NamedColumnDefinion.Compile(LanguageContext);
                while (field.MoveNext()) yield return field.Current;
                if (i < fields.Length - 1) yield return ", ";
            }
            yield return ") ";
#if DEBUG
            yield return "\r\n";
#endif
            if (Output != null)
            {
                var clause = LanguageContext.InsertReturningClause_at_Values(Output);
                while (clause.MoveNext()) yield return clause.Current;
            }

            var what = SourceData.InsertSourceDefinion.Compile(LanguageContext);
            while (what.MoveNext()) yield return what.Current;

            if (Output != null)
            {
                var clause = LanguageContext.InsertReturningClause_at_End(Output);
                while (clause.MoveNext()) yield return clause.Current;
            }
        }
    }

}

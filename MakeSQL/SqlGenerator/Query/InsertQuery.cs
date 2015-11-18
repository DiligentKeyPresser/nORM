using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class InsertQuery : IQuery
    {
        private QualifiedIdentifier Into;
        private IColumnDefinion[] fields;
        private IInsertSource SourceData;

        // some methods like `Where` change state just after cloning, so we cannot assign the builder in a constructor
        public Builder Query => new Builder(Compile);

        public InsertQuery(QualifiedIdentifier Into, IColumnDefinion[] Fields, IInsertSource From)
        {
            this.Into = Into;
            fields = Fields;
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
            var what = SourceData.InsertSourceDefinion.Compile(LanguageContext);
            while (what.MoveNext()) yield return what.Current;
        }
    }

}

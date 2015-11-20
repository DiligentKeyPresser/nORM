using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class DeleteQuery : IQuery
    {
        private QualifiedIdentifier Table;
#warning string???
        private string where;

        // some methods like `Where` change state just after cloning, so we cannot assign the builder in a constructor
        public Builder Query => new Builder(Compile);

        /// <summary> Creates a simple select query which can be extended or used as a subquery </summary>
        /// <param name="Source"> A qualified name of table/view or a subquery</param>
        public DeleteQuery(QualifiedIdentifier Table, string where)
        {
            this.Table = Table;
            this.where = where;
        }

        private IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return "DELETE FROM ";

            var From = Table.SourceDefinion.Compile(LanguageContext);
            while (From.MoveNext()) yield return From.Current;

            yield return " WHERE ";
            yield return where;
        }
    }

}

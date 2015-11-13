using MakeSQL.Internals;
using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class SelectQuery : Builder, IQuery
    {
        private ISelectSource source;
        private IFieldDefinion[] fields;
        private string[] where;

#warning add overload with QualifiedIdentifier
        /// <summary>
        /// Creates a simple select query which can be extended or used as a subquery
        /// </summary>
        /// <param name="Source"> A qualified name of table/view or</param>
        public SelectQuery(ISelectSource Source, params IFieldDefinion[] Fields)
        {
            source = Source;
            fields = Fields;
        }

        public SelectQuery Clone() => new SelectQuery(source, fields) { where = where };

        public IQuery NewSelect(params IFieldDefinion[] NewFields)
        {
            var clone = Clone();
            clone.fields = NewFields;
            return clone;
        }

        public SelectQuery Where(string Clause)
        {
            var clone = Clone();

            var new_where = new string[where.Length + 1];
            Array.Copy(where, new_where, where.Length);
            new_where[where.Length] = Clause;

            clone.where = new_where;
            return clone;
        }

        internal override IEnumerator<string> Compile(QueryFactory LanguageContext)
        {
            throw new NotImplementedException();
        }

        string IQuery.Build(QueryFactory LanguageContext) => Build(LanguageContext);
    }

}

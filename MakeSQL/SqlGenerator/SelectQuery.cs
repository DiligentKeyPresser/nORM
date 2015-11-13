using MakeSQL.Internals;
using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class SelectQuery : Builder, IQuery
    {
        private ISelectSource source;
        private IColumnDefinion[] fields;
        private string[] where;

#warning add overload with QualifiedIdentifier
        /// <summary>
        /// Creates a simple select query which can be extended or used as a subquery
        /// </summary>
        /// <param name="Source"> A qualified name of table/view or a subquery</param>
        public SelectQuery(ISelectSource Source, params IColumnDefinion[] Fields)
        {
            source = Source;
            fields = Fields;
        }

        /// <summary>
        /// Creates a copy of the given query
        /// </summary>
        public SelectQuery Clone() => new SelectQuery(source, fields) { where = where };

        /// <summary>
        /// Creates a SELECT query (based on the given one) with different columns 
        /// </summary>
        public IQuery NewSelect(params IColumnDefinion[] NewFields)
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
            yield return "SELECT ";
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i].Builder.Compile(LanguageContext);
                while (field.MoveNext()) yield return field.Current;                  
                if (i < fields.Length - 1) yield return ", ";
            }
            yield return " FROM ";
            var From = source.Builder.Compile(LanguageContext);
            while (From.MoveNext()) yield return From.Current;

            if (where.Length > 0)
            {
                yield return " WHERE ";
                bool brackets = where.Length > 1;

                for (int i = 0; i < where.Length; i++)
                {
                    if (brackets) yield return "(";
                    yield return where[i];
                    if (brackets) yield return ")";

                    if (i < where.Length - 1) yield return " AND ";
                }
            }
        }

        string IQuery.Build(QueryFactory LanguageContext) => Build(LanguageContext);
    }

}

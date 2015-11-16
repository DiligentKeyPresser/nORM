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
#warning long?
        private int? top = null;

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
        public SelectQuery Clone() => new SelectQuery(source, fields) { where = where, top = top };

        /// <summary>
        /// Creates a SELECT query (based on the given one) with different columns 
        /// </summary>
        public SelectQuery NewSelect(params IColumnDefinion[] NewFields)
        {
            var clone = Clone();
            clone.fields = NewFields;
            return clone;
        }

        public SelectQuery Where(string Clause)
        {
            if (top == null)
            {
                var clone = Clone();

                string[] new_where = null;
                if (where == null) new_where = new string[] { Clause };
                else
                {
                    new_where = new string[where.Length + 1];
                    Array.Copy(where, new_where, where.Length);
                    new_where[where.Length] = Clause;
                }

                clone.where = new_where;
                return clone;
            }
            else
            {
#warning constant name :(
                var clone = new SelectQuery(this.AS("T"), fields);
                var new_where = new string[] { Clause };
                clone.where = new_where;
                return clone;
            }
        }

        public SelectQuery Top(int count)
        {
            if (top.HasValue && top < count) return this;
            else
            {
                var clone = Clone();
                clone.top = count;
                return clone;
            }
        }

        public SelectQuery Any()
        {
#warning constant name :(
            return new SelectQuery(Top(1).NewSelect(new Constant(1).AS("A")).AS("T"), new Cast(new SQLFunctionCall(SqlFunction.Count, new Constant(1)), typeof(bool)));
        }

        internal override IEnumerator<string> Compile(SQLContext LanguageContext)
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

            if (where?.Length > 0)
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

        public new string Build(SQLContext LanguageContext) => base.Build(LanguageContext);
    }

}

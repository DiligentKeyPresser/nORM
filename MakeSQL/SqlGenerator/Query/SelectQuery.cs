﻿using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class SelectQuery : IQuery, IInsertSource
    {
        private ISelectSource source;
        private IColumnDefinion[] fields;
#warning string???
        private string[] where;
#warning long?
        private int? top = null;

        // some methods like `Where` change state just after cloning, so we cannot assign the builder in a constructor
        public Builder Query => new Builder(Compile);

        public Builder InsertSourceDefinion => Query;

#warning add overload with QualifiedIdentifier
        /// <summary> Creates a simple select query which can be extended or used as a subquery </summary>
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
                var clone = new SelectQuery(this.name("T"), fields);
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
#warning constant names :(
            return new SelectQuery(
                this.Top(1).NewSelect(1.literal().name("A")).name("T"), 
                new Cast(Function.Count.invoke(1.literal()), typeof(bool)).name("Result"));
        }

        private IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return "SELECT ";
            if (top.HasValue)
            {
                yield return "TOP ";
                yield return top.ToString();
                yield return " ";
            }
#if DEBUG
            if (fields.Length > 1) yield return "\r\n   ";
#endif
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i].NamedColumnDefinion.Compile(LanguageContext);
                while (field.MoveNext()) yield return field.Current;                  
                if (i < fields.Length - 1) yield return ", ";
            }
#if DEBUG
            yield return "\r\n";
#endif
            yield return " FROM ";
#if DEBUG
            yield return "\r\n   ";
#endif
            var From = source.SourceDefinion.Compile(LanguageContext);
            while (From.MoveNext())
            {
                var current = From.Current;
#if DEBUG
                current = current.Replace("\r\n", "\r\n   ");
#endif
                yield return current;
            }

            if (where?.Length > 0)
            {
#if DEBUG
                yield return "\r\n";
#endif
                yield return " WHERE ";
#if DEBUG
                yield return "\r\n   ";
#endif
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
    }

}

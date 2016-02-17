using System;
using System.Collections.Generic;

#warning SelectQuery is ISelectSource now. Does this lead to further simplifications?

namespace MakeSQL
{
    // IQuery        : a SelectQuery can be compiled directly to an SQL command.
    // ISelectSource : any SELECT query can be directly used as a subquery.

    public sealed class SelectQuery : IQuery, ISelectSource, IInsertSource
    {
        #region ISelectSource

        // This code allows SelectQuery to be an ISelectSource.
        //
        // We can check if the query fetches just all the rows from the source table
        // and decide if we need use the underlying table directly instead
        // of an actual subquery. 
        //
        // We can ignore a row ordering in a subquery, though.

        /// <summary>
        /// Chooses and returns an appropriate sql text builder for using as a subquery.
        /// </summary>
        Builder ISelectSource.SourceDefinion => IsDirectFetch ? source.SourceDefinion : new Builder(CompileAsASubquery);

        /// <summary>
        /// Tells if the query retrieves all the rows from the source table without filtering/joining.
        /// This property is supposed to ignore if row order is changed.
        /// </summary>
        private bool IsDirectFetch => top == null && join == null && where == null;

        /// <summary>
        /// Turns a query into a subquery usable in FROM and JOIN clauses
        /// </summary>
        private IEnumerator<string> CompileAsASubquery(SQLContext LanguageContext)
        {
            yield return "(";
#if DEBUG
            yield return "\r\n ";
#endif
            var subquery = Compile(LanguageContext);
            while (subquery.MoveNext()) yield return subquery.Current;
#if DEBUG
            yield return "\r\n";
#endif
            yield return ")";
        }

        #endregion

        private sealed class JoinClause
        {
            public enum Type
            {
                INNER,
           //     LEFT,
           //     RIGHT,
           //     CROSS
            }

            public Type JoinType { get; }

            public ISelectSource Source { get; }

#warning very poor approach
            public string InnerKey { get; }
#warning very poor approach
            public string OuterKey { get; }

            public LocalToken Alias { get; }

            private JoinClause()
            {
                Alias = new LocalToken();
            }

#warning very poor approach
            public JoinClause(Type JoinType, ISelectSource Source, string OuterKey, string InnerKey)
                : this()
            {
                this.Source = Source;
                this.InnerKey = InnerKey;
                this.OuterKey = OuterKey;
                this.JoinType = JoinType;
            }
        }

        /// <summary> Where to select from. </summary>
        private readonly ISelectSource source;

#warning please, notice:
        // Source_token should normally be used under next conditions only:
        //        * join
        //        * subquery
        private readonly LocalToken source_token;

        private JoinClause[] join;

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

#warning take from source if tokenized
            source_token = new LocalToken();
        }

        /// <summary> Creates a copy of the given query. </summary>
        public SelectQuery Clone() => new SelectQuery(source, fields) { where = where, top = top, join = join };

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
                var clone = new SelectQuery(this, fields);
                var new_where = new string[] { Clause };
                clone.where = new_where;
                return clone;
            }
        }

#warning very poor approach
        public SelectQuery InnerJoin(ISelectSource Source, string OuterKey, string InnerKey)
        {
            var clone = Clone();

            var NewJoinClause = new JoinClause(JoinClause.Type.INNER, Source, OuterKey, InnerKey);

            if (clone.join == null) clone.join = new JoinClause[] { NewJoinClause };
            else
            {
                var joinLength = clone.join.Length;
                var new_join = new JoinClause[joinLength + 1];
                if (joinLength > 0) Array.Copy(clone.join, new_join, joinLength);
                new_join[joinLength] = NewJoinClause;

                clone.join = new_join;
            }
            return clone;
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
                this.Top(1).NewSelect(1.literal().name("A")), 
                new Cast(Function.Count.invoke(1.literal()), typeof(bool)).name("Result"));
        }

        private IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            // We must determine if 'AS' statements will be used.
            bool SourceTokenIsUsed = join != null || source is SelectQuery;

            // We create LocalTokenizationContext if we gonna use 'AS' only.
            var TokenBag = SourceTokenIsUsed ? new LocalTokenizationContext() : null;

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
                if (SourceTokenIsUsed)
                {
                    yield return TokenBag[source_token];
                    yield return ".";
                }

                var field = fields[i].NamedColumnDefinion.Compile(LanguageContext);
                while (field.MoveNext()) yield return field.Current;                  
                if (i < fields.Length - 1) yield return ", ";
            }
#if DEBUG
            yield return "\r\n";
#endif
            {// FROM clause
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

                if (SourceTokenIsUsed)
                {
                    yield return " AS ";
                    yield return TokenBag[source_token];
                }
            }

            if (join != null)
            {// JOIN
                foreach (var j in join)
                {
#if DEBUG
                    yield return "\r\n";
#endif
                    switch (j.JoinType)
                    {
                        case JoinClause.Type.INNER:
                            yield return " INNER JOIN ";
                            break;
                        default: throw new NotImplementedException("This join type has not been implemented yet.");
                    }

                    var From = j.Source.SourceDefinion.Compile(LanguageContext);
                    while (From.MoveNext()) yield return From.Current;

                    yield return " AS ";
                    yield return TokenBag[j.Alias];
                    yield return " ON ";

                    yield return TokenBag[source_token];
                    yield return ".";
                    yield return j.OuterKey;
                    yield return " = ";
                    yield return TokenBag[j.Alias];
                    yield return ".";
                    yield return j.InnerKey;
                }
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

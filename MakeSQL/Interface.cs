using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MakeSQL
{

#warning separate file
    namespace Internals
    {
        /// <summary>
        /// Composite query component
        /// </summary>
        public interface IBuildable
        {
            Builder Builder { get; }
        }

        /// <summary>
        /// Basic class for composite query components
        /// </summary>
        public abstract class Builder : IBuildable
        {
            internal Builder() { }

            Builder IBuildable.Builder => this;

            internal abstract IEnumerator<string> Compile(QueryFactory LanguageContext);

            internal string Build(QueryFactory LanguageContext)
            {
                var builder = new StringBuilder();
                var enumerator = Compile(LanguageContext);

                while (enumerator.MoveNext())
                    builder.Append(enumerator.Current);
                return builder.ToString();                                    
            }
        }
    }

#warning separate file
    public sealed class QualifiedIdentifier : Internals.Builder, ISelectSource
    {
        /// <summary> Name of the object </summary>
        public string Identifier { get; }

        /// <summary> Schema name </summary>
        public string Schema { get; }

        /// <summary> Cached representation of the name in different SQL flavors </summary>
        private readonly Dictionary<QueryFactory, string> CachedNames = new Dictionary<QueryFactory, string>();

        /// <summary> for debug purposes </summary>
        public override string ToString() => $"{Schema ?? "[no-schema]" }::{Identifier}";

        /// <summary>
        /// Gets a text representation of the identifier for a given SQL flavor
        /// </summary>
        public string Escape(QueryFactory LanguageContext)
        {
            string cached = null;
            if (!CachedNames.TryGetValue(LanguageContext, out cached))
            {
                var builder = new StringBuilder();

                if (!string.IsNullOrEmpty(Schema))
                {
                    builder.Append(LanguageContext.LeftEscapingSymbol);
                    builder.Append(Schema);
                    builder.Append(LanguageContext.RightEscapingSymbol);
                    builder.Append(".");
                }
                builder.Append(LanguageContext.LeftEscapingSymbol);
                builder.Append(Identifier);
                builder.Append(LanguageContext.RightEscapingSymbol);

                CachedNames[LanguageContext] = cached = builder.ToString();
            }
            return cached;
        }

        internal override IEnumerator<string> Compile(QueryFactory LanguageContext)
        {            
            yield return Escape(LanguageContext);
        }

        #region basic comparsion

        public override int GetHashCode() => Identifier.GetHashCode();

        public override bool Equals(object obj)
        {
            var another = obj as QualifiedIdentifier;
            return another == null ? ReferenceEquals(obj, this) : Identifier == another.Identifier && Schema == another.Schema;
        }

        #endregion

        public QualifiedIdentifier(string schema, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name", "Qualified name should not be empty.");
            Identifier = name;
            Schema = schema;
        }

        #region Conversion

        private static readonly Regex Parser = new Regex(@"^[\[`""]?(?<schema>\w*)[\]`""]?[\.:]+[\[`""]?(?<name>\w+)[\]`""]?$");

#warning test this!
        /// <summary>
        /// Converts string in format 'schema.name' into a QualifiedIdentifier using regex
        /// </summary>
        public static implicit operator QualifiedIdentifier(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) throw new ArgumentNullException("source", "Qualified name should not be empty.");
            var firstchar = source[0];

            if (firstchar == '.') return new QualifiedIdentifier(null, source.Substring(1));

            var parsed = Parser.Match(source);
            return new QualifiedIdentifier(parsed.Groups["schema"].Value, parsed.Groups["name"].Value);
        }

        #endregion
    }

    public interface ISelectSource : Internals.IBuildable
    {
        
    }

    public abstract class QueryFactory
    {
        public abstract ISelectQuery Select(ISelectSource Source);

        internal virtual string LeftEscapingSymbol => "\"";
        internal virtual string RightEscapingSymbol => "\"";
    }

    /// <summary>
    /// A query
    /// </summary>
    public interface IQuery
    {
        /// <summary>
        /// Builds a query from scratch
        /// </summary>
        /// <param name="LanguageContext"> A factory corresponding to current SQL flavor </param>
        string Build(QueryFactory LanguageContext);
    }

    public interface ISelectQuery : IQuery, Internals.IBuildable
    {
        ISelectQuery Clone();
        ISelectQuery Where(string Clause);
        IQuery MakeCount();
        IQuery MakeLongCount();
        IQuery MakeAny();
    }




}

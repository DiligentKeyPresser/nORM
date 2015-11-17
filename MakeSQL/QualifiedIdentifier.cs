using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MakeSQL
{
#warning name can be assigned to an object which is not a valid ISelectSource
    public sealed class QualifiedIdentifier : Buildable, ISelectSource
    {
#warning validation?
        /// <summary> Name of the object </summary>
        public string Identifier { get; }

#warning validation?
        /// <summary> Schema name </summary>
        public string Schema { get; }

#warning ??
        public Buildable Definion => this;

        /// <summary> Cached representation of the name in different SQL flavors </summary>
        private readonly Dictionary<SQLContext, string> CachedNames = new Dictionary<SQLContext, string>();

        /// <summary> for debug purposes </summary>
        public override string ToString() => $"{Schema}.{Identifier}";

        /// <summary>
        /// Gets a text representation of the identifier for a given SQL flavor
        /// </summary>
        public string Escape(SQLContext LanguageContext)
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

        internal override IEnumerator<string> Compile(SQLContext LanguageContext)
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

}
using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class LocalIdentifier : Internals.Builder, IColumnDefinion
    {
#warning validation?
        /// <summary> Name of the object </summary>
        public string Identifier { get; }

        /// <summary> Cached representation of the name in different SQL flavors </summary>
        private readonly Dictionary<SQLContext, string> CachedNames = new Dictionary<SQLContext, string>();

        /// <summary> for debug purposes </summary>
        public override string ToString() => Identifier;

        /// <summary>
        /// Gets a text representation of the identifier for a given SQL flavor
        /// </summary>
        public string Escape(SQLContext LanguageContext)
        {
            string cached = null;
            if (!CachedNames.TryGetValue(LanguageContext, out cached))
                CachedNames[LanguageContext] = cached = string.Concat(LanguageContext.LeftEscapingSymbol, Identifier, LanguageContext.RightEscapingSymbol);
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
            return another == null ? ReferenceEquals(obj, this) : Identifier == another.Identifier;
        }

        #endregion

        public LocalIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name", "Local name should not be empty.");
            Identifier = name;
        }

        public static implicit operator LocalIdentifier(string source) => new LocalIdentifier(source);
    }
}
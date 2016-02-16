using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class Star : IColumnDefinion
    {
        public Builder NamedColumnDefinion => ColumnDefinion;

        public Builder ColumnDefinion { get; }

        /// <summary> for debug purposes </summary>
        public override string ToString() => "*";

        /// <summary>
        /// Gets a text representation of the identifier for a given SQL flavor
        /// </summary>
        public string Escape(SQLContext LanguageContext) => "*";

        private IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return Escape(LanguageContext);
        }

        private Star()
        {
            ColumnDefinion = new Builder(Compile);
        }

        public static Star Instance { get; } = new Star();
    }
}
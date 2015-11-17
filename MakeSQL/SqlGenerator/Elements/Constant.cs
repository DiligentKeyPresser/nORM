using System;
using System.Collections.Generic;

namespace MakeSQL
{
    /// <summary> An SQL literal </summary>
    public sealed class Constant : IUnnamedColumnDefinion
    {
        public object Value { get; }

        public Builder ColumnDefinion { get; }

        internal Constant(object Value)
        {
            this.Value = Value;
            ColumnDefinion = new Builder(Compile);
        }

        private IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            var literal = LanguageContext.EscapeLiteral(Value);
            while (literal.MoveNext()) yield return literal.Current;
        }
    }
}

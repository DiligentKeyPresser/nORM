using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public class Constant : Buildable, IUnnamedColumnDefinion
    {
        public object Value { get; }

        public Buildable Definion => this;

        public Constant(object Value) { this.Value = Value; }

        internal override IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            var literal = LanguageContext.EscapeLiteral(Value);
            while (literal.MoveNext()) yield return literal.Current;
        }
    }
}

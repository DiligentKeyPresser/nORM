using MakeSQL.Internals;
using System.Collections.Generic;

namespace MakeSQL
{
#warning must be wrapped into renamed column
    public class Constant : Builder, IColumnDefinion
    {
        public object Value { get; }

        public Constant(object Value) { this.Value = Value; }

        internal override IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            var literal = LanguageContext.EscapeLiteral(Value);
            while (literal.MoveNext()) yield return literal.Current;
        }
    }
}

using MakeSQL.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakeSQL
{
    public class Constant : Builder, IFieldDefinion
    {
        public object Value { get; }

        public Constant(object Value) { }

        internal override IEnumerator<string> Compile(QueryFactory LanguageContext)
        {
            var literal = LanguageContext.EscapeLiteral(Value);
            while (literal.MoveNext()) yield return literal.Current;
        }
    }
}

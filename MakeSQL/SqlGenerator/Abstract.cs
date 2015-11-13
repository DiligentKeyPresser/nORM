using System;
using MakeSQL.Internals;
using System.Collections.Generic;

namespace MakeSQL
{
    public abstract class QueryFactory
    {
        internal QueryFactory() { }

        internal virtual string GetFunctionName(SqlFunction Function)
        {
            switch (Function)
            {
                case SqlFunction.Count: return "COUNT";
                default: throw new NotSupportedException($"The given function (`{Function.ToString()}`) is not supporthed in the surrent context.");
            }
        }

        internal virtual IEnumerator<string> EscapeLiteral(object Value)
        {
            if (Value.GetType() == typeof(int))
            {
                yield return Value.ToString();
                goto stop;
            }
            throw new NotSupportedException($"The type of the given literal (`{Value.GetType().Name}`) is not supporthed in the surrent context.");
            stop:;
        }

        internal virtual string LeftEscapingSymbol => "\"";
        internal virtual string RightEscapingSymbol => "\"";
    }
}
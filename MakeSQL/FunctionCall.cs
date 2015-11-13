using MakeSQL.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakeSQL
{
    public enum SqlFunction
    {
        Count,
        CountBig
    }

    public class SQLFunctionCall : Builder, IFieldDefinion
    {
        public SqlFunction Function { get; }

        private readonly IFieldDefinion[] Arguments;

        public SQLFunctionCall(SqlFunction Func, params IFieldDefinion[] Args)
        {
            Function = Func;
            Arguments = Args;
        }

        internal override IEnumerator<string> Compile(QueryFactory LanguageContext)
        {
            yield return LanguageContext.GetFunctionName(Function);
            yield return "( ";
            for (int i = 0; i < Arguments.Length; i++)
            {
                var arg = Arguments[i].Builder.Compile(LanguageContext);
                while (arg.MoveNext()) yield return arg.Current;
                if (i < Arguments.Length - 1) yield return ", ";
            }
            yield return " )";
        }
    }
}

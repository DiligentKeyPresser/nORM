using MakeSQL.Internals;
using System.Collections.Generic;

namespace MakeSQL
{
    public enum SqlFunction
    {
        Count,
        CountBig
    }

    public class SQLFunctionCall : Builder, IColumnDefinion
    {
        public SqlFunction Function { get; }

        private readonly IColumnDefinion[] Arguments;

        public SQLFunctionCall(SqlFunction Func, params IColumnDefinion[] Args)
        {
            Function = Func;
            Arguments = Args;
        }

        internal override IEnumerator<string> Compile(SQLContext LanguageContext)
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

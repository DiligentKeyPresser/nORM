using System.Collections.Generic;

namespace MakeSQL
{
    public enum SqlFunction
    {
        Count,
        CountBig
    }

    public class FunctionCall : Buildable, IUnnamedColumnDefinion
    {
        public SqlFunction Function { get; }

#warning ??
        public Buildable Definion => this;

        private readonly IUnnamedColumnDefinion[] Arguments;

#warning do a renamed column return text without AS here?
        public FunctionCall(SqlFunction Func, params IUnnamedColumnDefinion[] Args)
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
                var arg = Arguments[i].Definion.Compile(LanguageContext);
                while (arg.MoveNext()) yield return arg.Current;
                if (i < Arguments.Length - 1) yield return ", ";
            }
            yield return " )";
        }
    }
}

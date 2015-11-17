using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public enum Function
    {
        Count,
        CountBig
    }

    public sealed class FunctionCall : IUnnamedColumnDefinion
    {
        private readonly IUnnamedColumnDefinion[] Arguments;
        public Function Function { get; }

        public Builder ColumnDefinion { get; }

        internal FunctionCall(Function Func, IUnnamedColumnDefinion[] Args)
        {
            Function = Func;
            Arguments = Args;
            ColumnDefinion = new Builder(Compile);
        }

        private IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return LanguageContext.GetFunctionName(Function);
            yield return "( ";
            for (int i = 0; i < Arguments.Length; i++)
            {
                var arg = Arguments[i].ColumnDefinion.Compile(LanguageContext);
                while (arg.MoveNext()) yield return arg.Current;
                if (i < Arguments.Length - 1) yield return ", ";
            }
            yield return " )";
        }
    }
}

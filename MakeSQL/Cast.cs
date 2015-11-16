using MakeSQL.Internals;
using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public class Cast : Builder, IColumnDefinion
    {
        public Type TargetType { get; }

        private readonly IColumnDefinion Argument;

        public Cast(IColumnDefinion Arg, Type TargetType)
        {
            this.TargetType = TargetType;
            Argument = Arg;
        }

        internal override IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return "CAST ( ";
            var arg = Argument.Builder.Compile(LanguageContext);
            while (arg.MoveNext()) yield return arg.Current;            
            yield return " AS ";
            yield return LanguageContext.GetTypeName(TargetType);
            yield return " )";
        }
    }
}
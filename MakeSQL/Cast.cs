using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public class Cast : Buildable, IUnnamedColumnDefinion
    {
        public Type TargetType { get; }

        public Buildable Definion => this;

        private readonly IUnnamedColumnDefinion Argument;

        public Cast(IUnnamedColumnDefinion Arg, Type TargetType)
        {
            this.TargetType = TargetType;
            Argument = Arg;
        }

        internal override IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return "CAST ( ";
            var arg = Argument.Definion.Compile(LanguageContext);
            while (arg.MoveNext()) yield return arg.Current;            
            yield return " AS ";
            yield return LanguageContext.GetTypeName(TargetType);
            yield return " )";
        }
    }
}
using System;
using System.Collections.Generic;

namespace MakeSQL
{
    /// <summary> SQL Cast operator </summary>
    public class Cast : Buildable, IUnnamedColumnDefinion
    {
        private readonly IUnnamedColumnDefinion Argument;
        public Type TargetType { get; }

#warning ???
        public Buildable Definion => this;

#warning do a renamed column return text without AS here?
        public Cast(IUnnamedColumnDefinion Arg, Type TargetType)
        {
            this.TargetType = TargetType;
            Argument = Arg;
        }

        internal override IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return "CAST (";
            var arg = Argument.Definion.Compile(LanguageContext);
            while (arg.MoveNext()) yield return arg.Current;            
            yield return " AS ";
            yield return LanguageContext.GetTypeName(TargetType);
            yield return ")";
        }
    }
}
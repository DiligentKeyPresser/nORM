using System;
using System.Collections.Generic;

namespace MakeSQL
{
    /// <summary> SQL Cast operator </summary>
    public sealed class Cast : IUnnamedColumnDefinion
    {
        private readonly IUnnamedColumnDefinion Argument;
        public Type TargetType { get; }

        public Builder ColumnDefinion { get; }

        internal Cast(IUnnamedColumnDefinion Arg, Type TargetType)
        {
            Argument = Arg;
            this.TargetType = TargetType;
            ColumnDefinion = new Builder(Compile);
        }

        private IEnumerator<string> Compile(SQLContext LanguageContext)
        {
            yield return "CAST (";
            var arg = Argument.ColumnDefinion.Compile(LanguageContext);
            while (arg.MoveNext()) yield return arg.Current;            
            yield return " AS ";
            yield return LanguageContext.GetTypeName(TargetType);
            yield return ")";
        }
    }
}
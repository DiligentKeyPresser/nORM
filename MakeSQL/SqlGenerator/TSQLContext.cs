using System;
using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class TSQLContext : SQLContext
    {
        #region Singleton
        private TSQLContext() { }
        public static TSQLContext Singleton { get; } = new TSQLContext();
        #endregion

        internal override string LeftEscapingSymbol => "[";
        internal override string RightEscapingSymbol => "]";

        internal override string GetFunctionName(Function Function)
        {
            switch (Function)
            {
                case Function.CountBig: return "COUNT_BIG";
                default: return base.GetFunctionName(Function);
            }
        }

        internal override string GetTypeName(Type type)
        {
            switch (type.Name)
            {
                case nameof(Boolean): return "BIT";
                default: return base.GetTypeName(type);
            }
        }

        internal override IEnumerator<string> InsertReturningClause_at_End(LocalIdentifier column)
        {
            yield break;
        }

        internal override IEnumerator<string> InsertReturningClause_at_Values(LocalIdentifier column)
        {
            yield return "OUTPUT inserted.";

            var id = column.ColumnDefinion.Compile(this);
            while (id.MoveNext())
                yield return id.Current;
#if DEBUG
            yield return "\r\n";
#endif
        }
    }
}

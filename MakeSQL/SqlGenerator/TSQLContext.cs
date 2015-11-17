using System;

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

    }
}

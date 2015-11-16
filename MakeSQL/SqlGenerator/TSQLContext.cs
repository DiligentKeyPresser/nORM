using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakeSQL.SqlGenerator
{
    class TSQLContext
    {
        #region Singleton
        private TSQLContext() { }
        public static TSQLContext Singleton { get; } = new TSQLContext();
        #endregion

        internal virtual string LeftEscapingSymbol => "[";
        internal virtual string RightEscapingSymbol => "]";

    }
}

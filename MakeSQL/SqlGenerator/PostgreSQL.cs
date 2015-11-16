using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakeSQL.SqlGenerator
{
    internal sealed class PostgreSQLContext : SQLContext
    {
        #region Singleton
        private PostgreSQLContext() { }
        public static PostgreSQLContext Singleton { get; } = new PostgreSQLContext();
        #endregion
    }
}

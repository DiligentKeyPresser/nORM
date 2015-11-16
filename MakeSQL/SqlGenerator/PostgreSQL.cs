using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakeSQL
{
    public sealed class PostgreSQLContext : SQLContext
    {
        #region Singleton
        private PostgreSQLContext() { }
        public static PostgreSQLContext Singleton { get; } = new PostgreSQLContext();
        #endregion        
    }
}

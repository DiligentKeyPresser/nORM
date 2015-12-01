using System.Collections.Generic;

namespace MakeSQL
{
    public sealed class PostgreSQLContext : SQLContext
    {
        #region Singleton
        private PostgreSQLContext() { }
        public static PostgreSQLContext Singleton { get; } = new PostgreSQLContext();
        #endregion

        internal override IEnumerator<string> InsertReturningClause_at_End(IColumnDefinion column)
        {
#if DEBUG
            yield return "\r\n";
#endif
            yield return " RETURNING ";
            var id = column.ColumnDefinion.Compile(this);
            while (id.MoveNext())
                yield return id.Current;

        }

        internal override IEnumerator<string> InsertReturningClause_at_Values(IColumnDefinion column)
        {
            yield break;
        }
    }
}

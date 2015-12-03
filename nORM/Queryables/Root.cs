using MakeSQL;

namespace nORM
{
    internal abstract class RowSource
    {
        internal readonly SelectQuery theQuery;

        internal RowSource(DatabaseContext ConnectionContext, SelectQuery Query)
        {
            Context = ConnectionContext;
            theQuery = Query;
        }

        internal DatabaseContext Context { get; }
    }
}
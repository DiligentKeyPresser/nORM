using System.Linq;

namespace nORM
{
    internal static class Materializer
    {
#warning lazyness should be checked
        /// <summary>
        /// Perfroms lazy materialization of IQueryable
        /// </summary>
        public static IQueryable<RowContract> Materialize<RowContract>(RowSource<RowContract> Source) => new EnumerableQuery<RowContract>(Source);
    }
}
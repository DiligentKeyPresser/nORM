using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace nORM
{
    internal static class Materializer
    {
#warning lazyness should be checked
        private sealed class LazyEnumerator<RowContract> : IEnumerable<RowContract>
        {
            private readonly RowSource<RowContract> Source;
            public LazyEnumerator(RowSource<RowContract> Source)
            {
                this.Source = Source;
            }

            public IEnumerator<RowContract> GetEnumerator()
            {
#warning действительно ли нет смысла возвращать обернутый reader?
                var materialized = Source.ToArray();
                foreach (var m in materialized) yield return m;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// Perfroms lazy materialization of IQueryable
        /// </summary>
        public static IQueryable<RowContract> Materialize<RowContract>(RowSource<RowContract> Source) => new LazyEnumerator<RowContract>(Source).AsQueryable();
    }
}
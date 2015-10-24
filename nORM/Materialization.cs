using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace nORM
{
    partial class RowSource<RowContract>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<RowContract> ExecuteInstant() => Context.ExecuteContract<RowContract>(GetSQL());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<RowContract> ExecuteDeferred()
        {
            var data = ExecuteInstant();
            foreach (var row in data) yield return row;
        }

        public IEnumerator<RowContract> GetEnumerator() => ExecuteInstant().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ExecuteInstant().GetEnumerator();

        /// <summary>
        /// This method creates an IQueryble instance with LinqToObjects provider 
        /// </summary>
#warning lazyness should be checked
        internal IQueryable<RowContract> Materialize() => new EnumerableQuery<RowContract>(ExecuteDeferred());
    }
}
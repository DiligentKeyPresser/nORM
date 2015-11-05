using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace nORM
{
    partial class RowSource<RowContract>
    {
#warning IEnumerator would be better
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<RowContract> ExecuteInstant() => Context.ExecuteContract<RowContract>(theQuery.ToString());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<RowContract> ExecuteDeferred() {
            var data = ExecuteInstant();
            foreach (var row in data) yield return row;
        }

        #region interface implementation
        public IEnumerator<RowContract> GetEnumerator() => ExecuteInstant().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ExecuteInstant().GetEnumerator();
        #endregion

        /// <summary>
        /// This method creates an IQueryble instance with LinqToObjects provider 
        /// </summary>
        internal IQueryable<RowContract> Materialize() => new EnumerableQuery<RowContract>(ExecuteDeferred());
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace nORM
{
    partial class RowSource<RowContract>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerator<RowContract> ExecuteInstant() => Context.ExecuteContract<RowContract>(theQuery.Query.Build(Context.QueryContext));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<RowContract> ExecuteDeferred() {
            var data = ExecuteInstant();
            while (data.MoveNext()) yield return data.Current;
        }

        #region interface implementation
        public IEnumerator<RowContract> GetEnumerator() => ExecuteInstant();
        IEnumerator IEnumerable.GetEnumerator() => ExecuteInstant();
        #endregion

        /// <summary>
        /// This method creates an IQueryble instance with LinqToObjects provider 
        /// </summary>
        internal IQueryable<RowContract> Materialize() => new EnumerableQuery<RowContract>(ExecuteDeferred());
    }
}
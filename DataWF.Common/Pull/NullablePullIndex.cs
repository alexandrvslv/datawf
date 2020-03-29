using DataWF.Common;
using System.Collections;

namespace DataWF.Data
{
    public class NullablePullIndex<T, K> : PullIndex<T, K?> where T : class, IPullHandler where K : struct
    {
        public NullablePullIndex(Pull pull, object nullKey, IComparer valueComparer, IEqualityComparer keyComparer = null)
            : base(pull, nullKey.GetType() == typeof(K) ? (K?)(K)nullKey : (K?)nullKey, valueComparer, keyComparer)
        {
        }
    }



}

using DataWF.Collections.Pulls;
using DataWF.Comparers;
using DataWF.Invokers;
using System.Collections.Generic;

namespace DataWF.Collections.Indexes
{
    public class PullIndex<T, K> : InvokerIndex<T, K>
        where K : notnull
        where T : IPullHandler
    {
        public PullIndex(Pull pull, IInvoker invoker, object nullKey)
            : this((GenericPull<K>)pull,
                  (IInvokerGetter<T, K>)invoker,
                  (K)nullKey,
                  DefaultComparer<T>.Default,
                  UniversalComparer<K>.Default)
        { }

        public PullIndex(GenericPull<K> pull, IInvokerGetter<T, K> invoker, K nullKey, IComparer<T> valueComparer, IUniversalComparer<K> keyComparer)
            : base(invoker, nullKey, valueComparer, keyComparer, true)
        {
            Pull = pull;
        }

        public GenericPull<K> Pull { get; }

        protected override int IndexOf(ArrayPointerList<T> list, T item)
        {
            var index = list.BinarySearch(item, valueComparer);
            if (index < 0)
            {
                index = list.IndexOf(item);
            }
            return index;
        }

        public override K GetKey(T item) => Pull.GetValue(in item.GetRefHandler());

        protected override void Add(ArrayPointerList<T> list, T item)
        {
            var index = list.BinarySearch(item, valueComparer);
            if (index < 0)
            {
                list.Insert(-index - 1, item);
            }
        }


    }
}

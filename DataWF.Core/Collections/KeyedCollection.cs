using DataWF.Collections.Indexes;
using DataWF.Invokers;
using System.Linq;

namespace DataWF.Collections
{
    public class IdCollection<T> : SelectableList<T>, IKeyedCollection<T> where T : IPrimaryKey
    {
        public T GetByKey(object id)
        {
            return items.FirstOrDefault(p => p.PrimaryKey?.Equals(id) ?? false);
        }
    }

    public class IdCollection<T, K> : SelectableList<T>, IKeyedCollection<T, K> where T : IPrimaryKey<K>
    {
        private IIndex<T, K> idIndex;

        public IdCollection()
        {
            idIndex = Indexes.AddIndex(InvokeProvider.GetInvoker<T, K>(nameof(IPrimaryKey.PrimaryKey)));
        }

        public T GetByKey(K id)
        {
            return idIndex.SelectOne(id);
        }
    }
}

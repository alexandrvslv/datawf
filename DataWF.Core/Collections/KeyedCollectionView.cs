using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Collections
{
    public class IdCollectionView<T, S> : IKeyedCollection<T>
        where T: S
    {
        public IdCollectionView(IKeyedCollection<S> source)
        {
            Source = source;
        }

        public IKeyedCollection<S> Source
        { get; }

        public int Count => Source.Count;

        public bool IsReadOnly => true;

        public void Add(T item)
        {
            Source.Add(item);
        }

        public void Clear()
        {
            Source.Clear();
        }

        public bool Contains(T item)
        {
            return Source.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public T? GetByKey(object id)
        {
            return (T?)Source.GetByKey(id);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Source.Select(p => (T)p).GetEnumerator();
        }

        public bool Remove(T item)
        {
            return Source.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

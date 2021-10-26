using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Common
{
    public class IdCollectionView<T, S> : IIdCollection<T>
    {
        public IdCollectionView(IIdCollection<S> source)
        {
            Source = source;
        }

        public IIdCollection<S> Source
        { get; }

        public int Count => Source.Count;

        public bool IsReadOnly => true;

        public void Add(T item)
        {
            Source.Add((S)(object)item);
        }

        public void Clear()
        {
            Source.Clear();
        }

        public bool Contains(T item)
        {
            return Source.Contains((S)(object)item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public T GetById(object id)
        {
            return (T)(object)Source.GetById(id);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Source.Select(p => (T)(object)p).GetEnumerator();
        }

        public bool Remove(T item)
        {
            return Source.Remove((S)(object)item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

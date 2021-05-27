using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class ListIndexes<T> : IListIndexes<T>
    {
        protected Dictionary<string, IListIndex<T>> indexes = new Dictionary<string, IListIndex<T>>(StringComparer.Ordinal);

        public bool Concurrent { get; set; }
        public int Count => indexes.Count;
        public IList<T> Source { set; get; }

        public void Add(string name, IListIndex<T> index)
        {
            indexes[name] = index;
        }

        IListIndex IListIndexes.Add(IInvoker invoker)
        {
            return Add(invoker);
        }

        public IListIndex<T, V> Add<V>(IInvoker<T, V> invoker)
        {
            return (IListIndex<T, V>)Add((IInvoker)invoker);
        }

        public IListIndex<T> Add(IInvoker invoker)
        {
            if (!indexes.TryGetValue(invoker.Name, out var index)
                && invoker is IInvokerExtension invokerExtension)
            {
                index = (IListIndex<T>)invokerExtension.CreateIndex<T>(Concurrent);
                indexes.Add(invoker.Name, index);
                index.Refresh(Source);
            }
            return index;
        }

        public IListIndex<T> GetIndex(string property)
        {
            return indexes.TryGetValue(property, out var index) ? index : null;
        }

        public ListIndex<T, K> GetIndex<K>(string property)
        {
            return (ListIndex<T, K>)GetIndex(property);
        }

        public void AddItem(T item)
        {
            foreach (var index in indexes.Values)
                index.Add(item);
        }

        public void RemoveItem(T item)
        {
            foreach (var index in indexes.Values)
                index.Remove(item);
        }

        public void Clear()
        {
            foreach (var index in indexes.Values)
                index.Clear();
        }

        public void AddItem(object item)
        {
            AddItem((T)item);
        }

        IListIndex IListIndexes.GetIndex(string property)
        {
            return GetIndex(property);
        }

        public void RemoveItem(object item)
        {
            RemoveItem((T)item);
        }


    }
}

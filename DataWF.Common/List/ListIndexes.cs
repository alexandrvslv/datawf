using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class ListIndexes<T> : IListIndexes<T>
    {
        protected Dictionary<string, ListIndex<T>> indexes = new Dictionary<string, ListIndex<T>>(StringComparer.OrdinalIgnoreCase);

        public void Add(IInvoker invoker)
        {
            if (!indexes.TryGetValue(invoker.Name, out var index))
            {
                index = new ListIndex<T>(invoker);
                indexes.Add(invoker.Name, index);
            }
        }

        public ListIndex<T> GetIndex(string property)
        {
            return indexes.TryGetValue(property, out var index) ? index : null;
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

using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface IListIndex
    {
        void Add(object item);
        void Clear();
        void Remove(object item);
        object SelectOne(object value);
        IEnumerable Scan(IQueryParameter parameter);
    }

    public interface IListIndex<T> : IListIndex
    {
        void Add(T item);
        void Remove(T item);
        IEnumerable<T> Scan(QueryParameter<T> parameter);
        new T SelectOne(object value);
        void Refresh(T item);
    }

    public interface IListIndex<T, K> : IListIndex<T>
    {
        T SelectOne(K value);
    }

}


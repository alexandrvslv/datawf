using System;

namespace DataWF.Common
{
    public interface IModelItem
    {
        IModelSchema Schema { get; }
    }

    public interface IModelTable : IModelItem, INamed
    {
        Type ItemType { get; }
        int TypeId { get; }

        bool Add(object item);
        bool Remove(object item);
    }

    public interface IModelTable<T> : IModelTable
    {
        bool Add(T item);
        bool Remove(T item);
    }
}
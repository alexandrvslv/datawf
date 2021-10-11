using System;
using System.Collections;
using System.Collections.Generic;

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
        IEnumerable Items { get; }

        bool Add(object item);
        bool Remove(object item);

    }

    public interface IModelTable<T> : IModelTable
    {
        new IEnumerable<T> Items { get; }

        bool Add(T item);
        bool Remove(T item);
    }
}
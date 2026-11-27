using DataWF.Collections.Query;
using DataWF.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Models;

public interface IModelTable : IModelItem, IList, IQueryProvider
{
    Type ItemType { get; }
    int TypeId { get; }
    ISerializeTypeInfo SerializeInfo { get; }
    IEnumerable<IModelColumn> Columns { get; }
    IModelColumn PrimaryKey { get; }
    IModelColumn ItemTypeKey { get; }

    IModelColumn GetColumn(string column);

    int GetTypeIndex(Type type);

    IQTable CreateQTable(string alias);
}

public interface IModelTable<T> : IModelTable, IList<T>, IQueryProvider<T>
{
    Type IModelTable.ItemType => typeof(T);

    new IQTable<T> CreateQTable(string alias) => new QTable<T>(this, alias);

    IQTable IModelTable.CreateQTable(string alias) => new QTable<T>(this, alias);
}

public interface IModelTable<T, K> : IModelTable<T>
    where T : new()
    where K : struct
{
}
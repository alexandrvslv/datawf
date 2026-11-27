using DataWF.Invokers;
using System;

namespace DataWF.Models;

public interface IModelSchema : IModelItem
{
    IModelProvider Provider { get; set; }
    IModelItemList<IModelTable> Tables { get; }

    IModelSchema IModelItem.Schema => this;

    IModelTable GetTableByTypeName(string word);
    IModelTable<T> GetTable<T>();
    IModelTable GetTable(string name);
    IModelTable GetTable(Type type);
    IModelTable GetTable(Type type, int typeId);
}
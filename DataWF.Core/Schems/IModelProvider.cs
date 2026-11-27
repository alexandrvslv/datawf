using DataWF.Access;
using System;

namespace DataWF.Models;

public interface IModelProvider : IAccessProvider
{
    IModelItemList<IModelSchema> Schems { get; }

    TableLoads<T, K> GetLoads<T, K>(IModelTable<T, K> webTable)
        where T : class, new()
        where K : struct;
    IModelSchema GetSchema(string name);
    T GetSchema<T>() where T : IModelSchema;

    IModelTable<T> GetTable<T>();
    IModelTable GetTable(string name);
    IModelTable GetTable(Type type);
    IModelTable GetTable(Type type, int typeId);
}
using System;

namespace DataWF.Common
{
    public interface IModelProvider
    {
        INamedList<IModelSchema> Schems { get; }

        IModelSchema GetSchema(string name);
        T GetSchema<T>() where T : IModelSchema;

        IModelTable<T> GetTable<T>();
        IModelTable GetTable(Type type);
        IModelTable GetTable(Type type, int typeId);
    }
}
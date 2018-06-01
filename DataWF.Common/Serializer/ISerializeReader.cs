using System;

namespace DataWF.Common
{

    public interface ISerializeReader : IDisposable
    {
        string CurrentName { get; }
        bool IsEmpty { get; }

        object Read(object element);
        object Read(object elemet, TypeSerializationInfo info);
        object ReadAttribute(string name, Type type);
        T ReadAttribute<T>(string name);
        bool ReadBegin();
        string ReadContent();
        Type ReadType();
        TypeSerializationInfo GetTypeInfo(Type type);
    }
}

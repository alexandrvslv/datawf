using System;

namespace DataWF.Common
{

    public interface ISerializeReader : IDisposable
    {
        string CurrentName { get; }
        object Read(object element);
        object ReadAttribute(string name, Type type);
        T ReadAttribute<T>(string name);
        bool ReadBegin();
        Type ReadType();

    }
}

using System;

namespace DataWF.Common
{
    public interface ISerializeWriter : IDisposable
    {
        void Write(object element);
        void Write(object element, string name, bool writeType);
        void WriteBegin(string name);
        void WriteAttribute(string name, object value);
        void WriteEnd();
        void WriteType(Type type);
    }
}

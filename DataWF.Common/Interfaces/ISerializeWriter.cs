using System;

namespace DataWF.Common
{
    public interface ISerializeWriter : IDisposable
    {
        void Write(object element);
        void Write<T>(T element);
    }
}

using System;

namespace DataWF.Common
{

    public interface ISerializeReader : IDisposable
    {
        object Read(object element);

        T Read<T>(T element);
    }
}

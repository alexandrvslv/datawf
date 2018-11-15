using Newtonsoft.Json.Serialization;
using System;

namespace DataWF.Common
{
    public interface IInvoker : IValueProvider, INamed
    {
        Type DataType { get; }

        Type TargetType { get; }

        bool CanWrite { get; }

        IListIndex CreateIndex();
    }

    public interface IInvoker<T, V> : IInvoker
    {
        V GetValue(T target);

        void SetValue(T target, V value);
    }
}

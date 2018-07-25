using Newtonsoft.Json.Serialization;
using System;

namespace DataWF.Common
{
    public interface IInvoker : IValueProvider
    {
        Type DataType { get; }

        Type TargetType { get; }

        string Name { get; set; }

        bool CanWrite { get; }

        IListIndex CreateIndex();
    }

    public interface IInvoker<T, V> : IInvoker
    {
        V GetValue(T target);

        void SetValue(T target, V value);
    }
}

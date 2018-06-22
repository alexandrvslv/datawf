using System;

namespace DataWF.Common
{
    public interface IInvoker
    {
        Type DataType { get; }

        Type TargetType { get; }

        string Name { get; set; }

        bool CanWrite { get; }

        object Get(object target);

        void Set(object target, object value);
    }

    public interface IInvoker<T, V> : IInvoker
    {
        V Get(T target);

        void Set(T target, V value);
    }
}

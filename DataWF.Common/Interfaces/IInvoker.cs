using Newtonsoft.Json.Serialization;
using System;
using System.Collections;

namespace DataWF.Common
{
    public interface IInvoker : IValueProvider, INamed
    {
        Type DataType { get; }
        Type TargetType { get; }
        bool CanWrite { get; }

        IListIndex CreateIndex(bool concurrent);
        IQueryParameter CreateParameter();
        InvokerComparer CreateComparer();

        bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision);
    }

    public interface IInvoker<T, V> : IInvoker
    {
        V GetValue(T target);
        void SetValue(T target, V value);

        bool CheckItem(T item, object typedValue, CompareType comparer, IComparer comparision);
    }
}

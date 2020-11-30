using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.ComponentModel;
using System.Text.Json;

namespace DataWF.Common
{
    public interface IInvoker : IValueProvider, INamed
    {
        Type DataType { get; }
        Type TargetType { get; }
        bool CanWrite { get; }

        bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision);
    }

    public interface IInvoker<in T, V> : IInvoker
    {
        V GetValue(T target);
        void SetValue(T target, V value);

        bool CheckItem(T item, object typedValue, CompareType comparer, IComparer comparision);
    }    
}

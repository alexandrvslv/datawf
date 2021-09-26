using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DataWF.Common
{
    public interface IQueryParameter : INotifyPropertyChanged, INamed
    {
        IInvoker Invoker { get; set; }
        CompareType Comparer { get; set; }
        IComparer Comparision { get; set; }
        LogicType Logic { get; set; }
        object Value { get; set; }
        object TypedValue { get; set; }
        bool IsEnabled { get; set; }
        bool IsGlobal { get; set; }
        QueryGroup Group { get; set; }
        string FormatName { get; set; }
        bool FormatEmpty { get; set; }
        bool FormatIgnore { get; set; }
        object Tag { get; set; }
        bool AlwaysTrue { get; set; }

        void Format(StringBuilder builder, bool logic);
        bool CheckItem(object item);
        bool CheckValue(object value);

        IEnumerable Search(IEnumerable items);

        IEnumerable Select(IEnumerable items, IListIndexes indexes = null);

        IEnumerable Distinct(IEnumerable items);

        IQueryParameter WithTag(object tag);
    }

    public interface IQueryParameter<T> : IQueryParameter
    {
        bool CheckItem(T item);

        IEnumerable<T> Search(IEnumerable<T> items);

        IEnumerable<T> Select(IEnumerable<T> items, IListIndexes<T> indexes = null);

        IEnumerable<T> Distinct(IEnumerable<T> items);

        new IQueryParameter<T> WithTag(object tag);
    }

    public interface IQueryParameter<T, V> : IQueryParameter<T>
    {
        new IInvoker<T, V> Invoker { get; set; }
        bool CheckValue(V value);

        new IQueryParameter<T, V> WithTag(object tag);
    }

    public interface IQueryFormatable
    {
        string Format();
    }
}
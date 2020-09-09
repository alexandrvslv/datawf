using System.Collections;
using System.ComponentModel;

namespace DataWF.Common
{
    public interface IQueryParameter : INotifyPropertyChanged
    {
        string Name { get; set; }
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
    }

    public interface IQueryFormatable
    {
        string Format();
    }
}
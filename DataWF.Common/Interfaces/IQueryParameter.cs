using System.Collections;

namespace DataWF.Common
{
    public interface IQueryParameter
    {
        string Name { get; set; }
        IInvoker Invoker { get; set; }
        CompareType Comparer { get; set; }
        IComparer Comparision { get; set; }
        LogicType Logic { get; set; }
        object Value { get; set; }
        object TypedValue { get; set; }
        bool IsEnabled { get; set; }
        bool GroupBegin { get; set; }
        bool GroupEnd { get; set; }
    }

    public interface IQueryFormatable
    {
        string Format();
    }
}
using System.Collections;

namespace DataWF.Common
{
    public interface IQueryParameter
    {
        string Property { get; set; }
        IInvoker Invoker { get; set; }
        CompareType Comparer { get; set; }
        IComparer Comparision { get; set; }
        LogicType Logic { get; set; }
        object Value { get; set; }
        object TypedValue { get; set; }
        bool IsEmpty { get; }
    }

    public interface IQueryFormatable
    {
        string Format();
    }
}
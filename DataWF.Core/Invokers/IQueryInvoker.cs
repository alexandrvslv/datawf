using DataWF.Collections.Indexes;
using DataWF.Collections.Query;
using DataWF.Comparers;

namespace DataWF.Invokers;

public interface IQueryInvoker : IInvoker
{
    IIndex CreateIndex(bool concurrent);
    IQBetween CreateQBetween(object min, object max);
    IQInvoker CreateQInvoker(string? tableAlias = null);
    IQValue CreateQValue(object value);
    bool CheckItem(object item, object typedValue, CompareOperator compareOpearator, IUniversalComparer comparision);
}

public interface IQueryInvoker<V> : IQueryInvoker, IInvokerGetter<V>
{
    IQBetween<V> CreateQBetween(V min, V max)
        => new QBetween<V>(min, max, this);

    IQBetween IQueryInvoker.CreateQBetween(object min, object max)
        => min is V typedMin && max is V typedMax
            ? CreateQBetween(typedMin, typedMax)
            : new QBetween<V>(min, max, this);

    new IQInvoker<V> CreateQInvoker(string? tableAlias = null)
        => new QInvoker<V>(this) { TableAlias = tableAlias };

    IQInvoker IQueryInvoker.CreateQInvoker(string? tableAlias)
        => CreateQInvoker(tableAlias);

    IQValue<V> CreateQValue(V value)
        => new QValue<V>(value);

    IQValue IQueryInvoker.CreateQValue(object value)
        => value is V typedValue
        ? CreateQValue(typedValue)
        : new QObjectValue(value);

    bool CheckItem(object item, V typedValue, CompareOperator compareOpearator, IUniversalComparer<V> comparision);

}

public interface IQueryInvoker<T, V> : IQueryInvoker<V>, IInvoker<T, V>
{
    new IIndex<T, V> CreateIndex(bool concurrent)
        => new InvokerIndex<T, V>(this, concurrent);

    IIndex IQueryInvoker.CreateIndex(bool concurrent)
        => CreateIndex(concurrent);

    bool CheckItem(T item, V typedValue, CompareOperator compareOpearator, IUniversalComparer<V> comparision)
        => ListHelper.CheckItem(GetValue(item), typedValue, compareOpearator, comparision);

    bool IQueryInvoker<V>.CheckItem(object item, V typedValue, CompareOperator compareOpearator, IUniversalComparer<V> comparision)
        => ListHelper.CheckItem(GetValue((T)item), typedValue, compareOpearator, comparision);

    bool IQueryInvoker.CheckItem(object item, object value, CompareOperator compareOpearator, IUniversalComparer comparision)
        => value is V typeValue
        ? ListHelper.CheckItem(GetValue((T)item), typeValue, compareOpearator, (IUniversalComparer<V>)comparision)
        : ListHelper.CheckItem(GetValue((T)item), value, compareOpearator, (IUniversalComparer<V>)comparision);

}

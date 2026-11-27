using DataWF.Invokers;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;

namespace DataWF.Comparers;

public sealed class InvokerComparer<T, V> : InvokerComparer<T, V, IInvoker<T, V>, UniversalComparer<V>>
{
    public InvokerComparer(IInvoker<T, V> invoker, ListSortDirection direction = ListSortDirection.Ascending)
        : this(invoker, UniversalComparer<V>.Default, direction)
    { }

    public InvokerComparer(IInvoker<T, V> invoker, UniversalComparer<V> comparer, ListSortDirection direction = ListSortDirection.Ascending)
        : base(invoker, comparer, direction)
    { }

    public InvokerComparer(string property, ListSortDirection direction = ListSortDirection.Ascending)
        : this(InvokeProvider.GetInvoker<T, V>(property), direction)
    { }

    public InvokerComparer(PropertyInfo info, ListSortDirection direction = ListSortDirection.Ascending)
        : this(info, null, direction)
    { }

    public InvokerComparer(PropertyInfo info, object? index, ListSortDirection direction = ListSortDirection.Ascending)
        : this(InvokeProvider.GetInvoker<T, V>(info, index), direction)
    { }

}
public class InvokerComparer<T, V, TInvoker, TComparer> : UniversalComparer<T>, IInvokerComparer<T, V>
    where TInvoker : IInvoker<T, V>
    where TComparer : class, IUniversalComparer<V>
{
    private ListSortDirection direction;
    protected int multiplier = 1;

    public InvokerComparer(TInvoker invoker, ListSortDirection direction = ListSortDirection.Ascending)
        : this(invoker, (TComparer)(object)UniversalComparer<V>.Default, direction)
    {

    }
    public InvokerComparer(TInvoker invoker, TComparer comparer, ListSortDirection direction = ListSortDirection.Ascending)
    {
        Invoker = invoker;
        Comparer = comparer;
        Direction = direction;
    }

    [JsonIgnore, XmlIgnore]
    public override string Name { get => Invoker.Name; set { } }

    public TInvoker Invoker { get; }

    public ListSortDirection Direction
    {
        get => direction;
        set
        {
            direction = value;
            multiplier = direction == ListSortDirection.Descending ? -1 : 1;
        }
    }

    public TComparer Comparer { get; }

    IUniversalComparer<V> IInvokerComparer<T, V>.Comparer
    {
        get => Comparer;
    }

    public override bool DefaultEquality => false;

    public override bool DefaultOrder => false;

    public int CompareValue(object? x, object? key) => CompareValue((T?)x, (V?)key);

    public int CompareValue(T? x, object? key) => CompareValue(x, (V?)key);

    public virtual int CompareValue(T? x, V? yValue)
    {
        if (x is null) return yValue is null ? 0 : multiplier * -1;

        var xValue = Invoker.GetValue(in x);
        var result = Comparer.Compare(xValue, yValue);
        return multiplier * result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Compare(T? x, T? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return multiplier * -1;
        if (y is null) return multiplier;

        V? xValue = Invoker.GetValue(in x);
        V? yValue = Invoker.GetValue(in y);
        var result = Comparer.Compare(xValue, yValue);
        return multiplier * result;
    }

    public override bool Equals(T? x, object? y)
    {
        if (x is null) return y is null;
        if (y is null) return Invoker.GetValue(in x) is null;

        return y switch
        {
            T t => Equals(x, t),
            V v => EqualsValue(x, v),
            _ => Comparer.Equals(Invoker.GetValue(in x), y) // Leverages cross-type Equals(V, object?)!
        };
    }

    public override bool Equals(T? x, T? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        V? xValue = Invoker.GetValue(in x);
        V? yValue = Invoker.GetValue(in y);
        return Comparer.Equals(xValue, yValue);
    }

    public bool EqualsValue(T? x, V? yValue)
    {
        if (x is null) return yValue is null;

        V? xValue = x == null ? default : Invoker.GetValue(in x);
        return Comparer.Equals(xValue, yValue);
    }

    public override int GetHashCode(T obj)
    {
        var objValue = Invoker.GetValue(obj);
        return objValue is null ? 0 : Comparer.GetHashCode(objValue);
    }

    public override bool Equals(object? obj)
    {
        return obj is IInvokerComparer<T, V> comp ? Equals(comp) : ReferenceEquals(this, obj);
    }

    private bool Equals(IInvokerComparer<T, V> comp)
    {
        return ReferenceEquals(Invoker, comp.Invoker)
            && Direction == comp.Direction;
    }

    public override int GetHashCode() => HashCode.Combine(Direction, Invoker);

    public void Format(StringBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Append(ToString());
    }

    public override string ToString()
    {
        return $" {Invoker.Name} {(Direction == ListSortDirection.Ascending ? "ASC" : "DESC")} ";
    }
}



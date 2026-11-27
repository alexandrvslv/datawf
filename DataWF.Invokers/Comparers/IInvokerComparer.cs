using DataWF.Invokers;
using System.Collections;
using System.ComponentModel;

namespace DataWF.Comparers;

public interface IInvokerComparer : IUniversalComparer, IInvokerDependant
{
    ListSortDirection Direction { get; set; }

    IUniversalComparer Comparer { get; }
}

public interface IInvokerComparer<in T> : IInvokerComparer, IUniversalComparer<T>
{

}

public interface IInvokerComparer<in T, V> : IInvokerComparer<T>, IInvokerGetterDependant<T, V>
{
    new IUniversalComparer<V> Comparer { get; }
    
    IUniversalComparer IInvokerComparer.Comparer
    {
        get => Comparer;
    }
    
    IInvokerGetter<T, V> IInvokerGetterDependant<T, V>.Invoker => Invoker;
}


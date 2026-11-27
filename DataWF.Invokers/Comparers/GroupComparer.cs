using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using DataWF.Invokers;
using DataWF.Invokers.Extensions;

namespace DataWF.Comparers;

/// <summary>
/// Comparer tree. Buid tree from list of any objects with selft type refenrece property.
/// </summary>
public sealed class GroupComparer<T> : InvokerComparer<T, T?, IInvoker<T, T?>, UniversalComparer<T?>>
{
    public static readonly GroupComparer<T> Instance = new();

    public GroupComparer(IInvoker<T, T?> gInvoker, UniversalComparer<T?> comparer)
        : base(gInvoker, comparer)
    { }

    public GroupComparer(string gName = GroupExtensions.GroupPropertyName)
        : this(InvokeProvider.GetInvoker<T, T?>(gName), UniversalComparer<T?>.Default)
    { }

    public override int Compare(T? x, T? y)
    {
        var result = GroupExtensions.Compare(x, y, Invoker, Comparer);
        return multiplier * result;
    }
}


using DataWF.Collections;
using DataWF.Comparers;
using DataWF.Invokers;
using DataWF.Invokers.Extensions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DataWF;

public static class GroupHelper
{
    private static Dictionary<Type, IInvoker> cacheTreeInvokers = new();

    public static IQueryInvoker GetTreeInvoker(Type type)
    {
        return (IQueryInvoker)InvokeProvider.GetInvoker(type, GroupExtensions.IsExpandedPropertyName);
    }

    public static void ApplyFilter(IFilterable filterable, Type itemType)
    {
        filterable.FilterQuery.Parameters.Add(TreeInvoker.Instance.CreateTreeParameter(itemType));
        filterable.FilterQuery.Orders.Add(TreeInvoker.Instance.CreateTreeComparer());
    }

    public static void ApplyFilter<T>(IFilterable<T> filterable) where T : IGroup
    {
        filterable.FilterQuery.Parameters.Add(TreeInvoker<T>.Instance.CreateTreeParameter<T>());
        filterable.FilterQuery.Orders.Add((InvokerComparer<T>)TreeInvoker<T>.Instance.CreateTreeComparer<T>());
    }


    
    
}



using System;
using System.Collections.Generic;

namespace DataWF;

public static class EventDelegateExtensions
{
    public static IEnumerable<T> GetContainers<T, D>(this ICollection<D>? handlers) where D : Delegate
    {
        if (handlers == null)
            yield break;
        foreach (var handler in handlers)
        {
            if (handler.Target is T container)
            {
                yield return container;
            }
        }
    }

    public static IEnumerable<T> GetContainers<T, D>(this D? handler) where D : Delegate
    {
        if (handler == null)
            yield break;
        foreach (var invocator in handler.GetInvocationList())
        {
            if (invocator.Target is T container)
            {
                yield return container;
            }
        }
    }

    
}

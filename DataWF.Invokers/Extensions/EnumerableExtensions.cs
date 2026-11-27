using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Invokers.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<T>? ToEnumerable<T>(this object? obj)
    {
        switch (obj)
        {
            case IEnumerable<T> enumerableObject:
                return enumerableObject;
            case IEnumerable enumerable:
                return enumerable.Cast<T>();
            case null:
                return null;
            default:
                return Enumerable.Repeat((T)obj, 1);
        }
    }
}


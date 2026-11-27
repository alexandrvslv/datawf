using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace DataWF.Invokers.Defaults;

public static class DictionaryMap<TKey, TValue>
    where TKey : notnull
{
    public static readonly Dictionary<Type, Action> Map = new()
    {
        { typeof(Dictionary<TKey, TValue>), static () => DictionaryInvokers<TKey,TValue>.RegisterInvokers() },
        { typeof(SortedDictionary<TKey, TValue>), static () => SortedDictionaryInvokers<TKey,TValue>.RegisterInvokers() },
        { typeof(SortedList<TKey, TValue>), static () => SortedListInvokers<TKey,TValue>.RegisterInvokers() },
        { typeof(ConcurrentDictionary<TKey, TValue>), static () => ConcurrentDictionaryInvokers<TKey,TValue>.RegisterInvokers() },
        { typeof(ImmutableDictionaryInvokers<TKey, TValue>), static () => ImmutableDictionaryInvokers<TKey,TValue>.RegisterInvokers() },
        { typeof(ImmutableSortedDictionaryInvokers<TKey, TValue>), static () => ImmutableSortedDictionaryInvokers<TKey,TValue>.RegisterInvokers() },
        { typeof(IDictionary<TKey, TValue>), static () => IDictionaryInvokers<TKey,TValue>.RegisterInvokers() },
        { typeof(IReadOnlyDictionary<TKey, TValue>), static () => IReadOnlyDictionaryInvokers<TKey,TValue>.RegisterInvokers() },
        { typeof(IImmutableDictionary<TKey, TValue>), static () => IImmutableDictionaryInvokers<TKey,TValue>.RegisterInvokers() },
    };
}

[Invoker(ToGenerate = typeof(KeyValuePair<,>), Instance = true)]
public partial class KeyValuePairInvokers<TKey, TValue>
    where TKey : notnull
{
}

[Invoker(Instance = true)]
public partial class DictionaryInvokers<TKey, TValue> : Dictionary<TKey, TValue>
    where TKey : notnull
{ }

[Invoker(Instance = true)]
public partial class SortedDictionaryInvokers<TKey, TValue> : SortedDictionary<TKey, TValue>
    where TKey : notnull
{ }

[Invoker(Instance = true)]
public partial class SortedListInvokers<TKey, TValue> : SortedList<TKey, TValue>
    where TKey : notnull
{ }

[Invoker(Instance = true)]
public partial class ConcurrentDictionaryInvokers<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
    where TKey : notnull
{ }

[Invoker(ToGenerate = typeof(ImmutableDictionary<,>), Instance = true)]
public partial class ImmutableDictionaryInvokers<TKey, TValue>
    where TKey : notnull
{ }

[Invoker(ToGenerate = typeof(ImmutableSortedDictionary<,>), Instance = true)]
public partial class ImmutableSortedDictionaryInvokers<TKey, TValue>
    where TKey : notnull
{ }

[Invoker(Instance = true)]
public partial interface IDictionaryInvokers<TKey, TValue> : IDictionary<TKey, TValue>
    where TKey : notnull
{ }

[Invoker(Instance = true)]
public partial interface IReadOnlyDictionaryInvokers<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{ }

[Invoker(Instance = true)]
public partial interface IImmutableDictionaryInvokers<TKey, TValue> : IImmutableDictionary<TKey, TValue>
    where TKey : notnull
{ }

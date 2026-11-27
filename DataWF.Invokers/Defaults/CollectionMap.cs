using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.Collections.Immutable;

namespace DataWF.Invokers.Defaults;

public static class CollectionMap<TEntry>
{
    public static readonly Dictionary<Type, Action> Map = new()
    {
        { typeof(List<TEntry>), static () => ListInvokers<TEntry>.RegisterInvokers() },
        { typeof(HashSet<TEntry>), static () => HashSetInvokers<TEntry>.RegisterInvokers() },
        { typeof(SortedSet<TEntry>), static () => SortedSetInvokers<TEntry>.RegisterInvokers() },
        { typeof(Queue<TEntry>), static () => QueueInvokers<TEntry>.RegisterInvokers() },
        { typeof(Stack<TEntry>), static () => StackInvokers<TEntry>.RegisterInvokers() },
        { typeof(LinkedList<TEntry>), static () => LinkedListInvokers<TEntry>.RegisterInvokers() },
        { typeof(ImmutableArray<TEntry>), static () => ImmutableArrayInvokers<TEntry>.RegisterInvokers() },
        { typeof(ImmutableList<TEntry>), static () => ImmutableListInvokers<TEntry>.RegisterInvokers() },
        { typeof(ImmutableHashSet<TEntry>), static () => ImmutableHashSetInvokers<TEntry>.RegisterInvokers() },
        { typeof(ImmutableSortedSet<TEntry>), static () => ImmutableSortedSetInvokers<TEntry>.RegisterInvokers() },
        { typeof(ImmutableQueue<TEntry>), static () => ImmutableQueueInvokers<TEntry>.RegisterInvokers() },
        { typeof(ImmutableStack<TEntry>), static () => ImmutableStackInvokers<TEntry>.RegisterInvokers() },
        { typeof(ConcurrentBag<TEntry>), static () => ConcurrentBagInvokers<TEntry>.RegisterInvokers() },
        { typeof(ConcurrentQueue<TEntry>), static () => ConcurrentQueueInvokers<TEntry>.RegisterInvokers() },
        { typeof(ConcurrentStack<TEntry>), static () => ConcurrentStackInvokers<TEntry>.RegisterInvokers() },
        { typeof(IList<TEntry>), static () => IListInvokers<TEntry>.RegisterInvokers() },
        { typeof(IReadOnlyList<TEntry>), static () => IReadOnlyListInvokers<TEntry>.RegisterInvokers() },
        { typeof(ICollection<TEntry>), static () => ICollectionInvokers<TEntry>.RegisterInvokers() },
        { typeof(IReadOnlyCollection<TEntry>), static () => IReadOnlyCollectionInvokers<TEntry>.RegisterInvokers() },
        { typeof(ISet<TEntry>), static () => ISetInvokers<TEntry>.RegisterInvokers() },
        { typeof(IReadOnlySet<TEntry>), static () => IReadOnlySetInvokers<TEntry>.RegisterInvokers() },
        { typeof(IImmutableList<TEntry>), static () => IImmutableListInvokers<TEntry>.RegisterInvokers() },
        { typeof(IImmutableSet<TEntry>), static () => IImmutableSetInvokers<TEntry>.RegisterInvokers() },
    };
}

[Invoker(Instance = true)]
public partial class ListInvokers<T> : List<T>
{ }

[Invoker(Instance = true)]
public partial class HashSetInvokers<T> : HashSet<T>
{ }

[Invoker(Instance = true)]
public partial class SortedSetInvokers<T> : SortedSet<T>
{ }

[Invoker(Instance = true)]
public partial class StackInvokers<T> : Stack<T>
{ }

[Invoker(Instance = true)]
public partial class QueueInvokers<T> : Queue<T>
{ }

[Invoker(Instance = true)]
public partial class PriorityQueueInvokers<TElement, TPriority> : PriorityQueue<TElement, TPriority>
{ }

[Invoker(Instance = true)]
public partial class LinkedListInvokers<T> : LinkedList<T>
{ }

[Invoker(ToGenerate = typeof(ImmutableArray<>), Instance = true)]
public partial class ImmutableArrayInvokers<T>
{ }

[Invoker(ToGenerate = typeof(ImmutableList<>), Instance = true)]
public partial class ImmutableListInvokers<T>
{ }

[Invoker(ToGenerate = typeof(ImmutableHashSet<>), Instance = true)]
public partial class ImmutableHashSetInvokers<T>
{ }

[Invoker(ToGenerate = typeof(ImmutableSortedSet<>), Instance = true)]
public partial class ImmutableSortedSetInvokers<T>
{ }

[Invoker(ToGenerate = typeof(ImmutableStack<>), Instance = true)]
public partial class ImmutableStackInvokers<T>
{ }

[Invoker(ToGenerate = typeof(ImmutableQueue<>), Instance = true)]
public partial class ImmutableQueueInvokers<T>
{ }

[Invoker(Instance = true)]
public partial class ConcurrentBagInvokers<T> : ConcurrentBag<T>
{ }

[Invoker(Instance = true)]
public partial class ConcurrentQueueInvokers<T> : ConcurrentQueue<T>
{ }

[Invoker(Instance = true)]
public partial class ConcurrentStackInvokers<T> : ConcurrentStack<T>
{ }

[Invoker(Instance = true)]
public partial interface ICollectionInvokers<T> : ICollection<T>
{ }

[Invoker(Instance = true)]
public partial interface IListInvokers<T> : IList<T>
{ }

[Invoker(Instance = true)]
public partial interface IReadOnlyCollectionInvokers<T> : IReadOnlyCollection<T>
{ }

[Invoker(Instance = true)]
public partial interface IReadOnlyListInvokers<T> : IReadOnlyList<T>
{ }

[Invoker(Instance = true)]
public partial interface ISetInvokers<T> : ISet<T>
{ }

[Invoker(Instance = true)]
public partial interface IReadOnlySetInvokers<T> : IReadOnlySet<T>
{ }

[Invoker(Instance = true)]
public partial interface IImmutableListInvokers<T> : IImmutableList<T>
{ }

[Invoker(Instance = true)]
public partial interface IImmutableSetInvokers<T> : IImmutableSet<T>
{ }
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace DataWF.Serialization.Elements;

public static class CollectionSerializers<TEntry>
{
    private static readonly Dictionary<Type, Func<IElementSerializer>> map = new()
    {
        { typeof(TEntry[]), static () => ArraySerializers.Register<TEntry>() },
        { typeof(List<TEntry>), static () => SerializeProvider.Register(ListSerializer<TEntry>.Instance) },
        { typeof(LinkedList<TEntry>), static () => SerializeProvider.Register(LinkedListSerializer<TEntry>.Instance) },
        { typeof(HashSet<TEntry>), static () => SerializeProvider.Register(HashSetSerializer<TEntry>.Instance) },
        { typeof(SortedSet<TEntry>), static () => SerializeProvider.Register(SortedSetSerializer<TEntry>.Instance) },
        { typeof(Queue<TEntry>), static () => SerializeProvider.Register(QueueSerializer<TEntry>.Instance) },
        { typeof(Stack<TEntry>), static () => SerializeProvider.Register(StackSerializer<TEntry>.Instance) },
        { typeof(ConcurrentBag<TEntry>), static () => SerializeProvider.Register(ConcurrentBagSerializer<TEntry>.Instance) },
        { typeof(ConcurrentQueue<TEntry>), static () => SerializeProvider.Register(ConcurrentQueueSerializer<TEntry>.Instance) },
        { typeof(ConcurrentStack<TEntry>), static () => SerializeProvider.Register(ConcurrentStackSerializer<TEntry>.Instance) },
        { typeof(ImmutableArray<TEntry>), static () => SerializeProvider.Register(ImmutableArraySerializer<TEntry>.Instance) },
        { typeof(ImmutableList<TEntry>), static () => SerializeProvider.Register(ImmutableListSerializer<TEntry>.Instance) },
        { typeof(ImmutableHashSet<TEntry>), static () => SerializeProvider.Register(ImmutableHashSetSerializer<TEntry>.Instance) },
        { typeof(IImmutableList<TEntry>), static () => SerializeProvider.Register(InterfaceImmutableListSerializer<TEntry>.Instance) },
        { typeof(IImmutableSet<TEntry>), static () => SerializeProvider.Register(InterfaceImmutableSetSerializer<TEntry>.Instance) },
        { typeof(ISet<TEntry>), static () => SerializeProvider.Register(InterfaceSetSerializer<TEntry>.Instance) },
        { typeof(IReadOnlySet<TEntry>), static () => SerializeProvider.Register(InterfaceReadOnlySetSerializer<TEntry>.Instance) },
        { typeof(IList<TEntry>), static () => SerializeProvider.Register(InterfaceListSerializer<TEntry>.Instance) },
        { typeof(IReadOnlyList<TEntry>), static () => SerializeProvider.Register(InterfaceReadOnlyListSerializer<TEntry>.Instance) },
        { typeof(ICollection<TEntry>), static () => SerializeProvider.Register(InterfaceCollectionSerializer<TEntry>.Instance) },
        { typeof(IReadOnlyCollection<TEntry>), static () => SerializeProvider.Register(InterfaceReadOnlyCollectionSerializer<TEntry>.Instance) },
        { typeof(IEnumerable<TEntry>), static () => SerializeProvider.Register(InterfaceEnumerableSerializer<TEntry>.Instance) },
    };

    public static void Add(IElementSerializer serializer)
    {
        map[serializer.TypeInfo.Type] = () => SerializeProvider.Register(serializer);
    }

    public static IElementSerializer<T?> Register<T>()
        where T : IEnumerable<TEntry>?
    {
        if (map.TryGetValue(typeof(T), out var func))
            return (IElementSerializer<T?>)func();
        var baseType = typeof(T).BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            if (map.TryGetValue(baseType, out func))
            {
                return (IElementSerializer<T?>)func();
            }
            baseType = baseType.BaseType;
        }

        throw new NotImplementedException(typeof(T).ToString());
    }


}


using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DataWF.Comparers;

namespace DataWF.Invokers.Extensions;

public static class GroupExtensions
{
    public const string GroupPropertyName = "Group";
    public const string IsExpandedPropertyName = "IsExpanded";

    [InlineArray(32)]
    private struct StackBuffer32<T>
    {
        private T _element0;
    }

    public ref struct AntiLoopSet<T>
    {
        private StackBuffer32<T> _buffer;
        private int _count;
        private readonly IEqualityComparer<T> _comparer;

        // Brent's Cycle-Finding state (for depth > 32)
        private T? _tortoise;
        private int _stepsTaken;
        private int _stepLimit;

        public AntiLoopSet() : this(null)
        { }

        public AntiLoopSet(IEqualityComparer<T>? comparer)
        {
            _comparer = comparer ?? UniversalComparer<T>.Default;
            _count = 0;
            _tortoise = default;
            _stepsTaken = 0;
            _stepLimit = 2; // Starts at power of 2
        }

        public readonly int Count => _count;

        public readonly IEqualityComparer<T> Comparer => _comparer;

        /// <summary>
        /// Returns true if safe to proceed; returns false if a loop is detected.
        /// Zero heap allocations at any depth. No Dispose() required.
        /// </summary>
        public bool TryPush(T item)
        {
            Span<T> span = _buffer;

            // 1. Check inline stack buffer (handles depths 1..32 with instant detection)
            for (int i = 0; i < _count; i++)
            {
                if (_comparer.Equals(span[i], item))
                    return false; // Loop detected
            }

            if (_count < span.Length)
            {
                span[_count++] = item;

                if (_count == span.Length)
                {
                    _tortoise = item;
                    _stepsTaken = 0;
                    _stepLimit = 2;
                }
                return true;
            }

            // 2. Depth > 32: Brent's Cycle-Finding Algorithm (0 allocations, no invoker needed)
            if (_comparer.Equals(_tortoise, item))
                return false; // Loop detected

            _stepsTaken++;
            if (_stepsTaken == _stepLimit)
            {
                _tortoise = item; // Teleport tortoise to current hare position
                _stepsTaken = 0;
                _stepLimit *= 2;  // Double the search window for cycles of any length
            }

            return true;
        }
    }

    public ref struct RefHashSet<T>
    {
        private StackBuffer32<T> _inlineItems;
        private T[]? _items;       // Rented when count > 32 (preserves insertion order for BFS/indexing)
        private int[]? _buckets;   // 1-based indices into _items (0 = empty slot)
        private int[]? _next;      // Collision chain links (0-based)
        private int _count;
        private readonly IEqualityComparer<T> _comparer;

        public RefHashSet() : this(null)
        {
        }

        public RefHashSet(IEqualityComparer<T>? comparer)
        {
            _comparer = comparer ?? UniversalComparer<T>.Default;
            _items = null;
            _buckets = null;
            _next = null;
            _count = 0;
        }

        public readonly int Count => _count;

        public readonly T this[int index] => _items is not null
            ? _items[index]
            : _inlineItems[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(T item)
        {
            // Tier 1: Fast L1 stack scan for <= 32 items (0 heap allocations)
            if (_buckets is null)
            {
                Span<T> inline = _inlineItems;
                for (int i = 0; i < _count; i++)
                {
                    if (_comparer.Equals(inline[i], item))
                        return false;
                }

                if (_count < inline.Length)
                {
                    inline[_count++] = item;
                    return true;
                }

                // Transition to Tier 2 (O(1) hash table via ArrayPool)
                UpgradeToHashTable(64);
            }

            // Tier 2: O(1) HashSet lookup on rented ArrayPool arrays
            int hashCode = item is null ? 0 : (_comparer.GetHashCode(item) & 0x7FFFFFFF);
            int bucketIndex = hashCode & (_buckets!.Length - 1); // Power-of-2 bitmask (faster than %)

            for (int i = _buckets[bucketIndex] - 1; i >= 0; i = _next![i])
            {
                if (_comparer.Equals(_items![i], item))
                    return false;
            }

            if (_count == _items!.Length)
            {
                ResizeHashTable(_count * 2);
                bucketIndex = hashCode & (_buckets!.Length - 1);
            }

            _items[_count] = item;
            _next![_count] = _buckets[bucketIndex] - 1;
            _buckets[bucketIndex] = _count + 1;
            _count++;
            return true;
        }

        private void UpgradeToHashTable(int initialCapacity)
        {
            _items = ArrayPool<T>.Shared.Rent(initialCapacity);
            _buckets = ArrayPool<int>.Shared.Rent(initialCapacity);
            _next = ArrayPool<int>.Shared.Rent(initialCapacity);

            // ArrayPool arrays are not zeroed; clear only the power-of-2 bucket range we use
            _buckets.AsSpan(0, initialCapacity).Clear();

            ReadOnlySpan<T> inline = _inlineItems;
            for (int i = 0; i < _count; i++)
            {
                T existing = inline[i];
                _items[i] = existing;

                int hashCode = existing is null ? 0 : (_comparer.GetHashCode(existing) & 0x7FFFFFFF);
                int bucketIndex = hashCode & (initialCapacity - 1);
                _next[i] = _buckets[bucketIndex] - 1;
                _buckets[bucketIndex] = i + 1;
            }
        }

        private void ResizeHashTable(int newCapacity)
        {
            // Note: Ensure newCapacity stays a power of 2
            var newItems = ArrayPool<T>.Shared.Rent(newCapacity);
            var newBuckets = ArrayPool<int>.Shared.Rent(newCapacity);
            var newNext = ArrayPool<int>.Shared.Rent(newCapacity);

            newBuckets.AsSpan(0, newCapacity).Clear();
            _items.AsSpan(0, _count).CopyTo(newItems);

            for (int i = 0; i < _count; i++)
            {
                T existing = newItems[i];
                int hashCode = existing is null ? 0 : (_comparer.GetHashCode(existing) & 0x7FFFFFFF);
                int bucketIndex = hashCode & (newCapacity - 1);
                newNext[i] = newBuckets[bucketIndex] - 1;
                newBuckets[bucketIndex] = i + 1;
            }

            ReturnBuffers();
            _items = newItems;
            _buckets = newBuckets;
            _next = newNext;
        }

        private void ReturnBuffers()
        {
            if (_items is not null)
                ArrayPool<T>.Shared.Return(_items, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            if (_buckets is not null)
                ArrayPool<int>.Shared.Return(_buckets);
            if (_next is not null)
                ArrayPool<int>.Shared.Return(_next);
        }

        public void Dispose()
        {
            ReturnBuffers();
            _items = null;
            _buckets = null;
            _next = null;
            _count = 0;
        }
    }

    public static void ExpandAll<T>(this T item,
        bool expand,
        string gName = GroupPropertyName,
        string eName = IsExpandedPropertyName)
    {
        item.ExpandAll(
            gInvoker: InvokeProvider.GetInvoker<T, T?>(gName),
            expandInvoker: InvokeProvider.GetInvoker<T, bool>(eName),
            expand: expand
        );
    }

    public static void ExpandAll<T, TBase>(this T item,
        IInvokerGetter<T, TBase?> gInvoker,
        IInvokerSetter<TBase, bool> expandInvoker,
        bool expand)
        where T : TBase
    {
        var antiLoop = new AntiLoopSet<TBase>();
        antiLoop.TryPush(item);

        var g = gInvoker.GetValue(item);
        while (g != null && antiLoop.TryPush(g))
        {
            expandInvoker.SetValue(g, expand);
            g = gInvoker.GetValue(g);
        }
    }

    public static void IsAllGroupsExpand<T>(this T item,
        string gName = GroupPropertyName,
        string eName = IsExpandedPropertyName)
    {
        item.IsAllGroupsExpand(
            gInvoker: InvokeProvider.GetInvoker<T, T?>(gName),
            expandInvoker: InvokeProvider.GetInvoker<T, bool>(eName)
        );
    }

    public static bool IsAllGroupsExpand<T>(this T item,
        IInvokerGetter<T, T?> gInvoker,
        IInvokerGetter<T, bool> expandInvoker)
    {
        var antiLoop = new AntiLoopSet<T>();
        antiLoop.TryPush(item);

        var g = gInvoker.GetValue(item);
        while (g != null && antiLoop.TryPush(g))
        {
            if (!expandInvoker.GetValue(g))
                return false;
            g = gInvoker.GetValue(g);
        }
        return true;
    }
    public static bool IsParent<T>(this T item,
        T parent,
        string gName = GroupPropertyName)
    {
        return item.IsParent(
            gInvoker: InvokeProvider.GetInvoker<T, T?>(gName),
            parent: parent
        );
    }
    public static bool IsParent<T>(this T item,
        IInvokerGetter<T, T?> gInvoker,
        T parent)
    {
        var antiLoop = new AntiLoopSet<T>();
        antiLoop.TryPush(item);

        var g = gInvoker.GetValue(item);
        while (g != null && antiLoop.TryPush(g))
        {
            if (antiLoop.Comparer.Equals(g, parent))
                return true;
            g = gInvoker.GetValue(g);
        }
        return false;
    }

    public static IEnumerable<T> GetAllParent<T>(this T item,
        bool addSender = false,
        string gName = GroupPropertyName)
    {
        return item.GetAllParent(
            gInvoker: InvokeProvider.GetInvoker<T, T?>(gName),
            addSender: addSender
        );
    }

    public static IEnumerable<T> GetAllParent<T>(this T item,
        IInvokerGetter<T, T?> gInvoker,
        bool addSender = false)
    {
        var result = new List<T>();
        var antiLoop = new AntiLoopSet<T>();
        antiLoop.TryPush(item);
        if (addSender)
            result.Add(item);
        var g = gInvoker.GetValue(item);
        while (g != null && antiLoop.TryPush(g))
        {
            result.Add(g);
            g = gInvoker.GetValue(g);
        }
        return result;
    }

    public static string GetFullName<T>(this T item,
        string separator = " ",
        string gName = GroupPropertyName)
    {
        return item.GetFullName<T, string>(
            member: "ToString",
            separator: separator,
            gName: gName
        );
    }

    public static string GetFullName<T, TMember>(this T item,
        string member,
        string separator = " ",
        string gName = GroupPropertyName)
    {
        return item.GetFullName(
            gInvoker: InvokeProvider.GetInvoker<T, T?>(gName),
            memberInvoker: InvokeProvider.GetInvoker<T, TMember>(member),
            separator: separator
        );
    }

    public static string GetFullName<T, TMember>(this T item,
        IInvokerGetter<T, T?> gInvoker,
        IInvokerGetter<T, TMember> memberInvoker,
        string separator = " ")
    {
        var antiLoop = new AntiLoopSet<T>();
        var rez = new List<string?>(item.Level(gInvoker) + 1);
        T? g = item;
        while (g != null && antiLoop.TryPush(g))
        {
            var val = memberInvoker.GetValue(g);
            rez.Insert(0, val?.ToString());
            g = gInvoker.GetValue(g);
        }
        return string.Join(separator, rez);
    }

    public static T TopGroup<T>(this T item, out int level,
        string gName = GroupPropertyName)
    {
        return item.TopGroup(
            gInvoker: InvokeProvider.GetInvoker<T, T?>(gName),
            out level);
    }

    public static T TopGroup<T>(this T item, IInvokerGetter<T, T?> gInvoker, out int level)
    {
        var antiLoop = new AntiLoopSet<T>();
        antiLoop.TryPush(item);
        level = 0;
        var g = item;
        while (gInvoker.GetValue(g) is T group && antiLoop.TryPush(group))
        {
            level++;
            g = group;
        }
        return g;
    }

    public static int Level<T>(this T item,
        string gName = GroupPropertyName)
    {
        return item.Level(
            gInvoker: InvokeProvider.GetInvoker<T, T?>(gName));
    }

    public static int Level<T>(this T item, IInvokerGetter<T, T?> gInvoker)
    {
        var antiLoop = new AntiLoopSet<T>();
        antiLoop.TryPush(item);
        int level = 0;
        var g = item;
        while (gInvoker.GetValue(g) is T group && antiLoop.TryPush(group))
        {
            level++;
            g = group;
        }
        return level;
    }

    public static IEnumerable<T> Sort<T>(IEnumerable<T> items)
    {
        var list = items.ToList();
        list.Sort(Compare<T>);
        return list;
    }

    // public static int Compare(object x, object y)
    //     => Compare(x as IGroup, y as IGroup);

    public static int Compare<T>(T? x, T? y)
        => Compare(x, y, comparer: null);

    public static int Compare<T>(T? x, T? y,
        IComparer<T>? comparer,
        string gName = GroupPropertyName)
    {
        return Compare<T>(x, y,
            gInvoker: InvokeProvider.GetInvoker<T, T>(gName),
            comparer: comparer);
    }

    public static int Compare<T>(T? x, T? y, IInvokerGetter<T, T?> gInvoker, IComparer<T>? comparer = null)
    {
        if (x == null)
            return (y == null) ? 0 : -1;
        if (y == null)
            return 1;
        var eqComparer = UniversalComparer<T>.Default;

        if (eqComparer.Equals(x, y))
            return 0;

        T ox = TopGroup(x, gInvoker, out int xLevel),
          oy = TopGroup(y, gInvoker, out int yLevel);

        if (!eqComparer.Equals(ox, oy))
        {
            return Compare(ox, oy, comparer, true);
        }
        ox = x; oy = y;
        int oxLevel = xLevel, oyLevel = yLevel;
        var loopX = new AntiLoopSet<T>(eqComparer); loopX.TryPush(ox);
        var loopY = new AntiLoopSet<T>(eqComparer); loopY.TryPush(oy);

        while (true)
        {
            var px = gInvoker.GetValue(ox);
            var py = gInvoker.GetValue(oy);

            if (eqComparer.Equals(px, py))
                break;
            if (oxLevel > oyLevel)
            {
                if (px == null || !loopX.TryPush(px))
                {
                    break;
                }
                ox = px;
                oxLevel--;
            }
            else if (oxLevel < oyLevel)
            {
                if (py == null || !loopY.TryPush(py))
                {
                    break;
                }
                oy = py;
                oyLevel--;
            }
            else
            {
                if (px == null || !loopX.TryPush(px)
                || py == null || !loopY.TryPush(py))
                {
                    break;
                }
                ox = px; oy = py;
                oxLevel--; oyLevel--;
            }
        }
        if (eqComparer.Equals(ox, oy))
        {
            return xLevel.CompareTo(yLevel);
        }

        return Compare(ox, oy, comparer, true);
    }


    public static IEnumerable<T> GetSubGroups<T>(this T group, IInvokerGetter<T, IEnumerable<T>> getItems, bool addSender = false)
    {
        var antiLoop = new RefHashSet<T>();
        var list = new List<T>();

        antiLoop.TryAdd(group);
        if (addSender)
        {
            list.Add(group);
        }
        var pending = new Stack<T>();
        pending.Push(group);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            foreach (var item in getItems.GetValue(current))
            {
                if (!antiLoop.TryAdd(item))
                {
                    continue;
                }

                list.Add(item);
                pending.Push(item);
            }
        }
        return list;
    }

    public static int Compare<T>(T x, T y, IComparer<T>? comp = null, bool checkHash = false)
    {
        var comparer = comp ?? UniversalComparer<T>.Default;
        var result = comparer.Compare(x, y);
        if (checkHash && result == 0 && comp != null)
        {
            result = x!.GetHashCode().CompareTo(y!.GetHashCode());
        }

        return result;
    }
}
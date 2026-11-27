using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;

namespace DataWF.Generator.Records
{
    public class EquatableList<T> : IEnumerable<T>, IEquatable<EquatableList<T>>
        where T: IEquatable<T>
    {
        public static readonly EquatableList<T> Empty = new(Array.Empty<T>());
        private T[] _items;

        public EquatableList(T[] array)
        {
            _items = array ?? throw new ArgumentNullException(nameof(array));
        }

        public EquatableList(IEnumerable<T> collection)
            : this(collection.ToArray())
        {
        }

        public int Count => _items.Length;
        public T this[int index] => _items[index];
        
        public EquatableList(int capacity)
        {
            _items = new T[capacity];
        }

        public bool Equals(EquatableList<T>? other)
        {
            return AsSpan().SequenceEqual(other == null ? ReadOnlySpan<T>.Empty : other.AsSpan());
        }

        public override bool Equals(object? obj) => Equals(obj as EquatableList<T>);

        public override int GetHashCode() => _items.Select(item => item?.GetHashCode() ?? 0).Aggregate((x, y) => x ^ y);

        public ReadOnlySpan<T> AsSpan() => _items.AsSpan();

        public ReadOnlySpan<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.AsEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _items.AsEnumerable().GetEnumerator();

        public static bool operator ==(EquatableList<T>? list1, EquatableList<T>? list2) => ReferenceEquals(list1, list2)
                || (list1 ?? Empty).Equals(list2);

        public static bool operator !=(EquatableList<T>? list1, EquatableList<T>? list2) => !(list1 == list2);
    }

}
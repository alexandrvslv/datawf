using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections
{
    public struct ArrayPointerSequence<T> : IEnumerable<ArrayPointerList<T>>
    {
        public static readonly ArrayPointerSequence<T> Empty = new((ArrayPointerSequenceEntry<T>)null);
        public readonly ArrayPointerSequenceEntry<T> First;

        public ArrayPointerSequence(ArrayPointerSequenceEntry<T> first) => First = first;

        public ArrayPointerSequence(ArrayPointerList<T> items) : this() => First = new ArrayPointerSequenceEntry<T>(items);

        public ArrayPointerSequenceEntry<T> Append(ArrayPointerList<T> items)
        {
            var entry = First;
            while (entry.Next != null)
                entry = entry.Next;
            return entry.Append(items);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<ArrayPointerList<T>> IEnumerable<ArrayPointerList<T>>.GetEnumerator() => GetEnumerator();

        public Enumerator GetEnumerator() => new Enumerator(First);

        public struct Enumerator : IEnumerator<ArrayPointerList<T>>, IEnumerator
        {
            private ArrayPointerSequenceEntry<T> first;
            private ArrayPointerSequenceEntry<T> current;

            public Enumerator(ArrayPointerSequenceEntry<T> first)
            {
                this.first = current = first;
            }

            public ArrayPointerList<T> Current => current.Items;

            object IEnumerator.Current => current.Items;

            public void Dispose() => first = null;

            public bool MoveNext() => (current = current?.Next) != null;

            public void Reset() => current = first;
        }
    }



}




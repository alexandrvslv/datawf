using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections
{
    public struct ReadOnlyList<T> : IReadOnlyList<T>
    {
        public static readonly IReadOnlyList<T> Empty = new ReadOnlyList<T>(Array.Empty<T>(), 0);

        private readonly T[] array;
        private readonly int count;

        public ReadOnlyList(T[] array, int count)
        {
            this.array = array;
            this.count = count;            
        }

        public T this[int index] => array[index];

        public int Count => count;


        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        public OneArrayEnumerator<T> GetEnumerator() => count == 0 
            ? OneArrayEnumerator<T>.Empty 
            : new OneArrayEnumerator<T>(array, count);
        
    }
}


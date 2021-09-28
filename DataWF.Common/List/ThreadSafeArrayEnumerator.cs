﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    [Serializable]
    public struct ThreadSafeArrayEnumerator<T> : IEnumerator<T>, IEnumerator
    {
        public static readonly ThreadSafeArrayEnumerator<T> Empty = new ThreadSafeArrayEnumerator<T>(null, 0);
        private int i;
        private uint count;
        private T[] items;
        private T single;
        public ThreadSafeArrayEnumerator(T single, int count)
        {
            i = -1;
            this.count = (uint)count;
            items = null;
            this.single = single;
        }

        public ThreadSafeArrayEnumerator(T[] items, int count)
        {
            i = -1;
            this.count = (uint)count;
            this.items = items;
            single = default(T);
        }

        public T Current => items != null ? items[i] : single;

        object IEnumerator.Current => Current;

        public void Dispose()
        { }

        public bool MoveNext()
        {
            return (uint)++i < count;
        }

        public void Reset()
        {
            i = -1;
        }
    }


}

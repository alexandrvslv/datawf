using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DataWF.Common
{
    public abstract class PullIndex : IDisposable
    {
        public abstract Pull BasePull { get; }
        public abstract void Refresh(IEnumerable items);
        public abstract void RefreshItem(object item);
        public abstract void RefreshSort(object item);
        public abstract void Add(object item);
        public abstract void Add(object item, object value);
        public void Add<T, V>(T item, V value) where T : class, IPullHandler
        {
            var pull = (PullIndex<T, V>)this;
            pull.Add(item, value);
        }

        public abstract void Remove(object item);
        public abstract void Remove(object item, object value);
        public void Remove<T, V>(T item, V value) where T : class, IPullHandler
        {
            var pull = (PullIndex<T, V>)this;
            pull.Remove(item, value);
        }
        public abstract IEnumerable Select(object value, CompareType compare);
        public abstract object SelectOne(object value);
        public abstract void Clear();
        public abstract void Dispose();
    }

}

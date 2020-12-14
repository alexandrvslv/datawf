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
    public abstract class PullIndex : IPullIndex, IDisposable
    {
        public abstract Pull BasePull { get; }
        public abstract void Refresh(IEnumerable items);
        public abstract void RefreshItem(object item);
        public abstract void RefreshSort(object item);
        public abstract void Add(object item);
        public abstract void Add(object item, object value);
        public abstract void Remove(object item);
        public abstract void Remove(object item, object value);
        public abstract IEnumerable SelectObjects(object value, CompareType compare);
        public abstract object SelectOneObject(object value);
        public abstract void Clear();
        public abstract void Dispose();
    }

}

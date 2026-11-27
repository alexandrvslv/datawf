using DataWF.Collections;
using DataWF.Collections.Indexes;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace DataWF
{
    public static class NetStat
    {
        private static readonly SelectableList<NetStatEntry> items = new() { AsyncNotification = true };
        private static IIndex<NetStatEntry, string> nameIndex;

        static NetStat()
        {
            nameIndex = items.Indexes.AddIndex(NetStatEntry.NameInvokerInstance);
        }

        public static SelectableList<NetStatEntry> Items => items;

        public static void Set(string name, int inc, long size)
        {
            var item = (NetStatEntry)nameIndex.SelectOne(name);
            if (item == null)
            {
                item = new NetStatEntry { Name = name };
                items.Add(item);
            }
            Interlocked.Add(ref item.count, inc);
            Interlocked.Add(ref item.length, size);
            items.OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

    }
}

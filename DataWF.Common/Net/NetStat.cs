using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace DataWF.Common
{
    public static class NetStat
    {
        private static readonly SelectableList<NetStatEntry> items = new SelectableList<NetStatEntry> { AsyncNotification = true };
        private static IListIndex<NetStatEntry, string> nameIndex;

        static NetStat()
        {
            nameIndex = (IListIndex<NetStatEntry, string>)items.Indexes.Add(NetStatEntry.NameInvoker.Instance);
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
            items.OnListChanged(NotifyCollectionChangedAction.Reset);
        }

    }
}

using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataWF.Common
{
    public static class NetStat
    {
        private static readonly SelectableList<NetStatItem> items = new SelectableList<NetStatItem>();

        static NetStat()
        {
            items.Indexes.Add(new ActionInvoker<NetStatItem, string>(nameof(NetStatItem.Name), (item) => item.Name));
        }

        public static SelectableList<NetStatItem> Items { get { return items; } }

        public static void Set(string name, int inc, long size)
        {
            var item = items.SelectOne(nameof(NetStatItem.Name), name);
            if (item == null)
            {
                item = new NetStatItem { Name = name };
                items.Add(item);
            }
            item.Count += inc;
            item.Length += size;
            items.OnListChanged(NotifyCollectionChangedAction.Reset);
        }

    }

    public class NetStatItem
    {
        public string Name { get; set; }
        public int Count { get; set; }
        [DefaultFormat("size")]
        public long Length { get; set; }
    }
}

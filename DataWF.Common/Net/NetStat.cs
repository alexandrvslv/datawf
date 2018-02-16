using System;
using System.ComponentModel;

namespace DataWF.Common
{
    public static class NetStat
    {
        private static SelectableList<NetStatItem> items = new SelectableList<NetStatItem>();

        static NetStat()
        {
            items.Indexes.Add(new Invoker<NetStatItem, string>(nameof(NetStatItem.Name), (item) => item.Name));
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
            items.OnListChanged(ListChangedType.Reset);
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

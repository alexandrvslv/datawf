using DataWF.Common;
using System.Collections.Generic;

namespace DataWF.Gui
{
    public class LayoutSortList : SelectableList<LayoutSort>
    {
        private class LayoutSortComparer : IComparer<LayoutSort>
        {
            public int Compare(LayoutSort x, LayoutSort y)
            {
                return x.CompareTo(y);
            }
        }

        public LayoutSortList()
        {
            Indexes.Add(new Invoker<LayoutSort, string>(nameof(LayoutSort.ColumnName), (item) => item.ColumnName));
            ApplySortInternal(new LayoutSortComparer());
        }

        public LayoutSortList(LayoutListInfo info)
            : this()
        {
            Info = info;
        }

        public LayoutListInfo Info { get; set; }

        public LayoutSort this[string name]
        {
            get { return SelectOne(nameof(LayoutSort.ColumnName), name); }
        }

        public bool IsGroup
        {
            get
            {
                foreach (LayoutSort sort in this)
                {
                    if (sort.IsGroup)
                        return true;
                    else
                        break;
                }
                return false;
            }
        }

        public override object NewItem()
        {
            var item = (LayoutSort)base.NewItem();
            item.Container = this;
            return item;
        }

        public LayoutSort Add(string name)
        {
            var sort = new LayoutSort()
            {
                ColumnName = name,
                Container = this
            };
            Add(sort);
            return sort;
        }

        public override void Add(LayoutSort item)
        {
            item.Order = Count;
            base.Add(item);
        }

        public bool Remove(string name)
        {
            var item = this[name];
            if (item != null)
            {
                return Remove(item);
            }
            return false;
        }

        public override bool Remove(LayoutSort item)
        {
            var flag = base.Remove(item);
            for (int i = 0; i < items.Count; i++)
            {
                items[i].Order = i;
            }
            return flag;
        }
    }
}

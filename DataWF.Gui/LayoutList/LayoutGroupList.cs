using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Gui
{
    public class PGroupComparer : IComparer<LayoutGroup>
    {
        public int Compare(LayoutGroup x, LayoutGroup y)
        {
            return x.IndexStart.CompareTo(y.IndexStart);
        }
    }

    public class LayoutGroupList : ICollection<LayoutGroup>
    {
        static readonly Invoker<LayoutGroup, string> textValueInvoker = new ActionInvoker<LayoutGroup, string>(nameof(LayoutGroup.TextValue), item => item.TextValue, (item, value) => item.TextValue = value);
        private SelectableList<LayoutGroup> items;
        private ILayoutList list;

        public LayoutGroupList(ILayoutList list)
        {
            this.list = list;
            items = new SelectableList<LayoutGroup>();
            items.Indexes.Add(textValueInvoker);
            items.ApplySort(new PGroupComparer());
        }

        public LayoutGroup this[object value]
        {
            get
            {
                foreach (LayoutGroup lg in items)
                    if (ListHelper.Compare(lg.Value, value, null, false) == 0)
                        return lg;
                return null;
            }
        }

        public void Add(LayoutGroup value)
        {
            items.Add(value);
        }

        public void Sort()
        {
            items.Sort();
        }

        public IEnumerator<LayoutGroup> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        public void RefreshGroup(int starIndex)
        {
            var count = list?.ListSource?.Count ?? 0;
            if (count == 0)
                return;
            var sorters = list.ListInfo.Sorters.Where(p => p.IsGroup).ToArray();
            string header = string.Empty;
            foreach (var item in sorters)
            {
                header += item.Column.Text + " ";
            }
            if (header.Length == 0)
                return;

            LayoutGroup lgroup = null;
            DateTime stamp = DateTime.Now;
            //int j = 0;
            for (int i = 0; i < count; i++)
            {
                object litem = list.ListSource[i];
                string format = null;
                foreach (var item in sorters)
                {
                    object val = list.ReadValue(litem, item.Column);
                    object f = list.FormatValue(litem, val, item.Column);

                    //	string val = ;
                    if (val is DateTime && ((string)f).Length > 10)
                    {
                        DateTime date = (DateTime)val;
                        f = Locale.Get("DateComapre", Helper.DateRevelantString(stamp, date));
                    }
                    format += f == null ? "" : (f.ToString() + " ");
                }
                if (lgroup != null && !string.Equals(lgroup.TextValue, format, StringComparison.Ordinal))
                {
                    lgroup.IndexEnd = i - 1;
                    lgroup = null;
                }
                if (lgroup == null)
                {
                    LayoutGroup exist = items.SelectOne(nameof(LayoutGroup.TextValue), format);
                    if (exist != null)
                    {
                        lgroup = exist;
                        lgroup.IndexStart = i;
                        lgroup.Stamp = stamp;
                    }
                    else
                    {
                        lgroup = new LayoutGroup()
                        {
                            Header = header,
                            Value = format,
                            TextValue = format,
                            IndexStart = i,
                            IndexEnd = i,
                            Info = list.ListInfo,
                            Stamp = stamp
                        };
                        items.Add(lgroup);
                    }
                }
            }
            if (lgroup != null)
            {
                lgroup.IndexEnd = list.ListSource.Count - 1;
            }
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Stamp != stamp)
                {
                    items.RemoveAt(i);
                    i--;
                }
            }
        }

        public void Clear()
        {
            foreach (var item in items)
                item.Dispose();
            items.Clear();
        }

        public int IndexOf(LayoutGroup g)
        {
            return items.IndexOf(g);
        }

        public LayoutGroup this[int index]
        {
            get { return items[index]; }
        }

        public bool Contains(LayoutGroup item)
        {
            return items.Contains(item);
        }

        public void CopyTo(LayoutGroup[] array, int arrayIndex)
        {
            items.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return items.Count; }
        }

        public bool IsReadOnly
        {
            get { return items.IsReadOnly; }
        }

        public bool Remove(LayoutGroup item)
        {
            return items.Remove(item);
        }
    }
}

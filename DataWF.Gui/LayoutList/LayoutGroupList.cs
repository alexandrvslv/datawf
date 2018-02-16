using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using DataWF.Common;

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
        private SelectableList<LayoutGroup> items = new SelectableList<LayoutGroup>();
        private ILayoutList list;

        public LayoutGroupList(ILayoutList list)
        {
            this.list = list;
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
            if (list == null || list.ListSource == null || list.ListInfo.Sorters.Count == 0)
                return;
            string header = string.Empty;
            foreach (var item in list.ListInfo.Sorters)
            {
                if (item.IsGroup)
                    header += item.Column.Text + " ";
                else
                    break;
            }
            if (header.Length == 0)
                return;

            LayoutGroup lg = null;
            DateTime stamp = DateTime.Now;
            int j = 0;
            for (int i = 0; i < list.ListSource.Count; i++)
            {
                object litem = list.ListSource[i];
                string format = null;
                foreach (var item in list.ListInfo.Sorters)
                {
                    if (item.IsGroup)
                    {
                        object val = list.ReadValue(litem, item.Column);
                        object f = list.FormatValue(litem, val, item.Column);

                        //	string val = ;
                        if (val is DateTime && ((string)f).Length > 10)
                        {
                            DateTime date = (DateTime)val;
                            if (stamp.Year != date.Year)
                                f = date.Year.ToString();
                            else if (stamp.Month != date.Month)
                                f = date.ToString("MMMM");
                            else if (stamp.Day == date.Day)
                                f = Locale.Get("DateComapre", "Today");
                            else if (stamp.Day == date.Day + 1)
                                f = Locale.Get("DateComapre", "Yestorday");
                            else if (stamp.Day - (int)stamp.DayOfWeek < date.Day)
                                f = Locale.Get("DateComapre", "This Week");
                            else
                                f = Locale.Get("DateComapre", "This Month");
                        }
                        format += f == null ? "" : (f.ToString() + " ");
                    }
                    else
                        break;
                }
                if (lg != null)
                {
                    var comp = ListHelper.Compare(lg.TextValue, format, null, false);
                    if (comp != 0)
                    {
                        lg.IndexEnd = i - 1;
                        lg = null;
                    }
                }
                if (lg == null)
                {

                    for (; j < items.Count; j++)
                    {
                        LayoutGroup item = items[j];
                        if (item == null)
                        {
                            items.RemoveAt(j);
                            j--;
                        }
                        else if (ListHelper.Compare(item.TextValue, format, null, false) == 0)
                        {
                            lg = item;
                            lg.IndexStart = i;
                            lg.Stamp = stamp;
                            break;
                        }
                    }
                }

                //if (lg != null)
                //{
                //    if (lg.IndexStart > i)
                //        lg.IndexStart = i;
                //    else if (lg.IndexEnd < i)
                //        lg.IndexEnd = i;
                //}
                if (lg == null)
                {
                    lg = new LayoutGroup()
                    {
                        Header = header,
                        Value = format,
                        TextValue = format,
                        IndexStart = i,
                        IndexEnd = i,
                        Info = list.ListInfo,
                        Stamp = stamp
                    };
                    items.Add(lg);
                    //index++;
                }
            }
            if (lg != null)
            {
                lg.IndexEnd = list.ListSource.Count - 1;
            }
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Stamp != stamp)
                {
                    items.RemoveAt(i);
                    i--;
                }
            }
            list.RefreshGroupsBound();
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

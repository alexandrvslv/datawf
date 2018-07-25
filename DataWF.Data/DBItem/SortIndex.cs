/*
 DBRowList.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>  

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Data
{
    public class SortIndex
    {
        private DBItem row;
        private DBTable list;
        private List<DBItem> index = new List<DBItem>();
        private IComparer comparer;
        private DBColumn column;
        //private string property;
        public SortIndex(DBTable list, string property)
        {
            this.row = list.NewItem(DBUpdateState.Default);
            this.list = list;
            //this.property = property;
            if (list.Columns.Contains(property))
            {
                this.column = list.Columns[property];
                this.comparer = new DBComparer(column, ListSortDirection.Ascending);
            }
            else
            {
                this.comparer = new InvokerComparer(typeof(DBItem), property, ListSortDirection.Ascending);
            }
            Refresh();
        }

        private void listOnListChanged(object sender, ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                case ListChangedType.Reset:
                    Refresh();
                    break;
                case ListChangedType.ItemAdded:
                    Add(list[e.NewIndex]);
                    break;
                case ListChangedType.ItemDeleted:
                    if (e.NewIndex >= 0)
                    {
                        Remove(list[e.NewIndex]);
                    }
                    break;
            }
        }

        public void Refresh()
        {
            index.Clear();
            foreach (DBItem item in list)
                index.Add(item);
            ListHelper.QuickSort(index, comparer);
        }

        public void Remove(DBItem row)
        {
            int iindex = ListHelper.BinarySearch(index, row, comparer);
            if (iindex >= 0 && index[iindex] == row)
                index.RemoveAt(iindex);
            else
                index.Remove(row);
        }

        public void Add(DBItem row)
        {
            int iindex = ListHelper.BinarySearch(index, row, comparer);

            if (iindex < 0)
                iindex = (-iindex) - 1;

            if (iindex > index.Count)
                iindex = index.Count;

            index.Insert(iindex, row);
        }

        private void SetValue(object value)
        {
            if (comparer is DBComparer)
                row[column] = value;
            else if (comparer is InvokerComparer)
            {
                if (value.GetType() != ((InvokerComparer)comparer).invoker.DataType)
                    value = Helper.TextParse(value.ToString(), ((InvokerComparer)comparer).invoker.DataType, "");
                ((InvokerComparer)comparer).invoker.SetValue(row, value);
            }
        }

        private void FillValues(List<DBItem> buf, int itemIndex, ref int first, ref int last)
        {
            first = last = itemIndex;
            for (int i = itemIndex + 1; i < index.Count; i++)
                if (comparer.Compare(index[itemIndex], index[i]) == 0)
                {
                    last = i;
                    buf.Add(index[i]);
                }
                else
                    break;
            buf.Add(index[itemIndex]);
            for (int i = itemIndex - 1; i >= 0; i--)
                if (comparer.Compare(index[itemIndex], index[i]) == 0)
                {
                    first = i;
                    buf.Add(index[i]);
                }
                else
                    break;
        }
        public List<DBItem> Select(object value, CompareType compare)
        {
            List<DBItem> buf = new List<DBItem>();
            if (index != null && index.Count == 0)
                return buf;

            if (compare.Type == CompareTypes.Is)
            {
                value = DBNull.Value;
                compare = new CompareType(CompareTypes.Equal, compare.Not);
            }

            SetValue(value);
            int itemIndex = ListHelper.BinarySearch(index, row, comparer);
            int first = -1;
            int last = -1;
            if (itemIndex >= 0)
            {
                FillValues(buf, itemIndex, ref first, ref last);
            }

            switch (compare.Type)
            {
                case CompareTypes.Equal:
                    if (itemIndex >= 0)
                        buf = (List<DBItem>)ListHelper.ORNOT(index, buf, null);
                    else
                        buf = (List<DBItem>)ListHelper.Copy(index);
                    break;
                case CompareTypes.Less:
                    if (itemIndex >= 0)
                        buf = (List<DBItem>)ListHelper.Copy(index, 0, first - 1);
                    else
                    {
                        if (ListHelper.Compare(row, index[0], comparer, false) < 0)
                            buf = (List<DBItem>)ListHelper.Copy(index);
                    }
                    break;
                case CompareTypes.LessOrEqual:
                    if (itemIndex >= 0)
                        buf = (List<DBItem>)ListHelper.Copy(index, 0, last);
                    else
                    {
                        if (ListHelper.Compare(row, index[0], comparer, false) < 0)
                            buf = (List<DBItem>)ListHelper.Copy(index);
                    }
                    break;
                case CompareTypes.GreaterOrEqual:
                    if (itemIndex >= 0)
                        buf = (List<DBItem>)ListHelper.Copy(index, first, index.Count - 1);
                    else
                    {
                        if (ListHelper.Compare(row, index[0], comparer, false) > 0)
                            buf = (List<DBItem>)ListHelper.Copy(index);
                    }
                    break;
                case CompareTypes.Greater:
                    if (itemIndex >= 0)
                        buf = (List<DBItem>)ListHelper.Copy(index, last + 1, index.Count - 1);
                    else
                    {
                        if (ListHelper.Compare(row, index[0], comparer, false) > 0)
                            buf = (List<DBItem>)ListHelper.Copy(index);
                    }
                    break;
            }
            return buf;
        }
        public void Dispose()
        {
            index.Clear();
            index.TrimExcess();
        }
    }
}

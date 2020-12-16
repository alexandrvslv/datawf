//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Data
{
    public class SortIndex
    {
        private readonly DBItem row;
        private readonly DBTable list;
        private readonly List<DBItem> index = new List<DBItem>();
        private readonly IComparer comparer;
        private readonly DBColumn column;
        //private string property;
        public SortIndex(DBTable table, string property)
        {
            this.row = table.NewItem(DBUpdateState.Default);
            this.list = table;
            //this.property = property;
            if (table.Columns.Contains(property))
            {
                column = table.Columns[property];
                comparer = column.CreateComparer(ListSortDirection.Ascending);
            }
            Refresh();
        }

        private void OnSourceListChanged(object sender, ListChangedEventArgs e)
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
            if (column != null)
                row[column] = value;
            else if (comparer is InvokerComparer)
            {
                if (value.GetType() != ((InvokerComparer)comparer).Invoker.DataType)
                    value = Helper.TextParse(value.ToString(), ((InvokerComparer)comparer).Invoker.DataType, "");
                ((InvokerComparer)comparer).Invoker.SetValue(row, value);
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
                        if (ListHelper.Compare(row, index[0], comparer) < 0)
                            buf = (List<DBItem>)ListHelper.Copy(index);
                    }
                    break;
                case CompareTypes.LessOrEqual:
                    if (itemIndex >= 0)
                        buf = (List<DBItem>)ListHelper.Copy(index, 0, last);
                    else
                    {
                        if (ListHelper.Compare(row, index[0], comparer) < 0)
                            buf = (List<DBItem>)ListHelper.Copy(index);
                    }
                    break;
                case CompareTypes.GreaterOrEqual:
                    if (itemIndex >= 0)
                        buf = (List<DBItem>)ListHelper.Copy(index, first, index.Count - 1);
                    else
                    {
                        if (ListHelper.Compare(row, index[0], comparer) > 0)
                            buf = (List<DBItem>)ListHelper.Copy(index);
                    }
                    break;
                case CompareTypes.Greater:
                    if (itemIndex >= 0)
                        buf = (List<DBItem>)ListHelper.Copy(index, last + 1, index.Count - 1);
                    else
                    {
                        if (ListHelper.Compare(row, index[0], comparer) > 0)
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

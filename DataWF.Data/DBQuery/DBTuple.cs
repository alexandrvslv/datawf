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
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace DataWF.Data
{
    public struct DBTuple<T> where T : DBItem
    {
        public T LeftItem;
        public DBTuple? RightItem;
    }

    public struct DBTuple
    {
        public DBTuple(DBItem leftItem)
        {
            Item0 = leftItem;
            Item1 = null;
            Item2 = null;
            Item3 = null;
            Item4 = null;
            Item5 = null;
            Item6 = null;
            Item7 = null;
            Count = 1;
        }

        public DBItem Item0;
        public DBItem Item1;
        public DBItem Item2;
        public DBItem Item3;
        public DBItem Item4;
        public DBItem Item5;
        public DBItem Item6;
        public DBItem Item7;
        public int Count;

        public DBItem this[int order]
        {
            get => Get(order);
            set => Set(order, value);
        }

        public DBItem Get(QTable qTable) => Get(qTable.Order) ?? Get(qTable.Table);

        public void Set(QTable table, DBItem item) => Set(table.Order, item);

        public DBItem Get(IDBTable table)
        {
            if (Item0?.Table == table) return Item0;
            if (Item1?.Table == table) return Item1;
            if (Item2?.Table == table) return Item2;
            if (Item3?.Table == table) return Item3;
            if (Item4?.Table == table) return Item4;
            if (Item5?.Table == table) return Item5;
            if (Item6?.Table == table) return Item6;
            if (Item7?.Table == table) return Item7;
            return null;
        }

        private void Set(int order, DBItem value)
        {
            if (order < 0)
                throw new ArgumentOutOfRangeException(nameof(order));
            switch (order)
            {
                case 0: Item0 = value; break;
                case 1: Item1 = value; break;
                case 2: Item2 = value; break;
                case 3: Item3 = value; break;
                case 4: Item4 = value; break;
                case 5: Item5 = value; break;
                case 6: Item6 = value; break;
                case 7: Item7 = value; break;
                default: throw new IndexOutOfRangeException();
            }
            Count = Math.Max(Count, order + 1);
        }

        private DBItem Get(int order)
        {
            if (order < 0)
                throw new ArgumentOutOfRangeException(nameof(order));
            switch (order)
            {
                case 0: return Item0;
                case 1: return Item1;
                case 2: return Item2;
                case 3: return Item3;
                case 4: return Item4;
                case 5: return Item5;
                case 6: return Item6;
                case 7: return Item7;
                default: throw new IndexOutOfRangeException();
            }
        }

        public DBTuple Clone()
        {
            var newTuple = new DBTuple();
            for (int i = 0; i < Count; i++)
                newTuple.Set(i, Get(i));
            return newTuple;
        }
    }
}
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
    public class DBTuple<T> : DBTuple where T : DBItem
    {
        public new T LeftItem { get => (T)base.LeftItem; set => base.LeftItem = value; }
    }
    
    public struct DBValueTuple
    {
        public DBItem LeftItem;
        public DBTuple RightItem;
    }

    public class DBTuple
    {
        public DBTuple()
        { }

        public DBItem LeftItem { get; set; }

        public DBTuple RightItem;

        public DBItem Get(QTable qTable) => Get(qTable.Order) ?? Get(qTable.Table);

        public void Set(QTable table, DBItem item) => Set(table.Order, item);

        public DBItem Get(IDBTable table)
        {
            var tuple = this;
            while (tuple != null)
            {
                if (tuple.LeftItem.Table == table)
                    return tuple.LeftItem;
                tuple = tuple.RightItem;
            }
            return null;
        }
        public DBItem this[int order]
        {
            get => Get(order);
            set => Set(order, value);
        }

        private void Set(int order, DBItem value)
        {
            if (order < 0)
                throw new ArgumentOutOfRangeException(nameof(order));
            var tuple = this;
            int index = 0;
            while (true)
            {
                if (index == order)
                {
                    tuple.LeftItem = value;
                    return;
                }
                tuple = tuple.RightItem ??= new DBTuple();
                index++;
            }
        }

        private DBItem Get(int order)
        {
            var tuple = this;
            int index = 0;
            while (tuple != null)
            {
                if (index == order)
                    return tuple.LeftItem;
                tuple = tuple.RightItem;
                index++;
            }
            return null;
        }

        public DBTuple Clone()
        {
            var newTuple = new DBTuple();
            var tuple = this;
            int index = 0;
            while (tuple != null)
            {
                newTuple.Set(index, tuple.LeftItem);
                tuple = tuple.RightItem;
                index++;
            }
            return newTuple;
        }
    }
}
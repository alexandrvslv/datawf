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
    public class QOrder : QItem
    {
        protected ListSortDirection direction = ListSortDirection.Ascending;
        private QItem item;

        public QOrder()
            : base()
        {
        }

        public QOrder(DBColumn column)
            : this(new QColumn(column))
        {
        }

        public QOrder(IInvoker invoker)
            : this(new QInvoker(invoker))
        {
        }

        public QOrder(QItem column)
        {
            Item = column;
        }

        public ListSortDirection Direction
        {
            get => direction;
            set
            {
                direction = value;
                //OnPropertyChanged();
            }
        }

        public QItem Item
        {
            get => item;
            set
            {
                if (item != value)
                {
                    item = value;
                    if (item != null)
                    {
                        item.Holder = this;
                    }
                    //OnPropertyChanged();
                }
            }
        }

        public override object GetValue(DBItem row)
        {
            return item.GetValue(row);
        }

        public override string Format(System.Data.IDbCommand command = null)
        {
            return Item is QColumn
                ? string.Format("{0} {1}", Item.Format(command), direction == ListSortDirection.Descending ? "desc" : "asc")
                : string.Empty;
        }

        public IComparer CreateComparer()
        {
            if (Item is QColumn column)
                return column.Column.CreateComparer(Direction);
            if (Item is QInvoker reflection)
                return reflection.Invoker.CreateComparer(Direction);
            return null;
        }

        public IComparer CreateComparer(Type type)
        {
            if (Item is QColumn column)
                return column.Column.CreateComparer(type, Direction);
            if (Item is QInvoker reflection)
                return reflection.Invoker.CreateComparer(type, Direction);
            return null;
        }

        public IComparer<T> CreateComparer<T>()
        {
            return (IComparer<T>)CreateComparer(typeof(T));
        }


    }
}


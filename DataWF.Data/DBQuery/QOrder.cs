﻿//  The MIT License (MIT)
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
    public class QOrder : QItem, IInvokerExtension
    {
        protected ListSortDirection direction = ListSortDirection.Ascending;
        private QItem column;

        public QOrder()
            : base()
        {
        }

        public QOrder(DBColumn column)
            : base()
        {
            Column = new QColumn(column);
        }

        public ListSortDirection Direction
        {
            get => direction;
            set
            {
                direction = value;
                OnPropertyChanged(nameof(Direction));
            }
        }

        public QItem Column
        {
            get => column;
            set
            {
                if (column != value)
                {
                    column = value;
                    if (column != null)
                    {
                        column.Holder = this;
                    }

                    OnPropertyChanged(nameof(Column));
                }
            }
        }

        public override object GetValue(DBItem row)
        {
            return column.GetValue(row);
        }

        public override string Format(System.Data.IDbCommand command = null)
        {
            return Column is QColumn
                ? string.Format("{0} {1}", Column.Format(command), direction == ListSortDirection.Descending ? "desc" : "asc")
                : string.Empty;
        }

        public IComparer CreateComparer()
        {
            if (Column is QColumn column)
                return column.Column.CreateComparer(Direction);
            if (Column is QReflection reflection
                && reflection.Invoker is IInvokerExtension invokerExtension)
                return invokerExtension.CreateComparer(reflection.Invoker.TargetType, Direction);
            return null;
        }

        public IListIndex CreateIndex(bool concurrent)
        {
            throw new NotImplementedException();
        }

        public IListIndex CreateIndex<T>(bool concurrent)
        {
            throw new NotImplementedException();
        }

        public IQueryParameter CreateParameter(Type type)
        {
            throw new NotImplementedException();
        }

        public QueryParameter<TT> CreateParameter<TT>()
        {
            throw new NotImplementedException();
        }

        public IComparer CreateComparer(Type type, ListSortDirection direction = ListSortDirection.Ascending)
        {
            if (Column is QColumn column)
                return column.Column.CreateComparer(type, direction);
            if (Column is QReflection reflection
                && reflection.Invoker is IInvokerExtension invokerExtension)
                return invokerExtension.CreateComparer(type, direction);
            return null;
        }

        public IComparer<TT> CreateComparer<TT>(ListSortDirection direction = ListSortDirection.Ascending)
        {
            return (InvokerComparer<TT>)CreateComparer(typeof(TT), direction);
        }
    }
}


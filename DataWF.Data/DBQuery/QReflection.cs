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
using System.Data;

namespace DataWF.Data
{
    public class QReflection : QItem, IInvokerExtension
    {
        [NonSerialized()]
        IInvoker invoker;

        public QReflection()
        { }

        public QReflection(string name)
            : base(name)
        { }

        public QReflection(IInvoker invoker)
            : this()
        {
            Invoker = invoker;
        }

        public IInvoker Invoker
        {
            get
            {
                if (invoker == null)
                    invoker = EmitInvoker.Initialize(typeof(DBItem), this.text);
                return invoker;
            }
            set
            {
                if (invoker != value)
                {
                    invoker = value;
                    Text = value?.Name;
                    OnPropertyChanged(nameof(Invoker));
                }
            }
        }

        public override object GetValue(DBItem row = null)
        {
            return Invoker?.GetValue(row);
        }

        public override string Format(IDbCommand command = null)
        {
            return command != null ? string.Empty : base.Format(command);
        }

        public IListIndex CreateIndex(bool concurrent)
        {
            return ((IInvokerExtension)Invoker).CreateIndex(concurrent);
        }

        public IListIndex CreateIndex<T>(bool concurrent)
        {
            return ((IInvokerExtension)Invoker).CreateIndex<T>(concurrent);
        }

        public IQueryParameter CreateParameter(Type type)
        {
            return ((IInvokerExtension)Invoker).CreateParameter(type);
        }

        public QueryParameter<TT> CreateParameter<TT>()
        {
            return ((IInvokerExtension)Invoker).CreateParameter<TT>();
        }

        public IComparer CreateComparer(Type type, ListSortDirection direction = ListSortDirection.Ascending)
        {
            return ((IInvokerExtension)Invoker).CreateComparer(type, direction);
        }

        public IComparer<TT> CreateComparer<TT>(ListSortDirection direction = ListSortDirection.Ascending)
        {
            return ((IInvokerExtension)Invoker).CreateComparer<TT>(direction);
        }
    }
}